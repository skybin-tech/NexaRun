using System.Net;
using System.Net.Mail;
using Amazon;
using Amazon.Runtime;
using Amazon.SimpleEmailV2;
using Amazon.SimpleEmailV2.Model;
using NexaRun.Shared.Config;
using NexaRun.Shared.Models;

namespace NexaRun.Daemon;

public class ProcessAlertService(ILogger<ProcessAlertService> logger)
{
    private readonly HashSet<string> _sentForProcess = new(StringComparer.OrdinalIgnoreCase);

    public void ClearDownState(string processName) => _sentForProcess.Remove(processName);

    public async Task SendProcessDownAsync(NexaProcess process, NexaRunSettings settings)
    {
        if (!settings.EmailAlertEnabled)
            return;

        if (!_sentForProcess.Add(process.Name))
            return;

        if (string.IsNullOrWhiteSpace(settings.AlertEmailTo))
        {
            logger.LogWarning("Email alert enabled but AlertEmailTo is empty");
            return;
        }

        if (string.IsNullOrWhiteSpace(settings.AlertEmailFrom))
        {
            logger.LogWarning("Email alert enabled but AlertEmailFrom is empty");
            return;
        }

        var body = BuildBody(process);
        var subject = $"[NexaRun] {process.Name} is down";

        try
        {
            if (EmailAlertProvider.Normalize(settings.EmailProvider) == EmailAlertProvider.SesSmtp)
                await SendViaSesSmtp(settings, subject, body);
            else
                await SendViaSesApi(settings, subject, body);

            logger.LogInformation(
                "Sent down alert for '{Name}' to {To} via {Provider}",
                process.Name,
                settings.AlertEmailTo,
                settings.EmailProvider);
        }
        catch (Exception ex)
        {
            _sentForProcess.Remove(process.Name);
            logger.LogError(ex, "Failed to send down alert for '{Name}'", process.Name);
        }
    }

    private static string BuildBody(NexaProcess process)
    {
        var reason = string.IsNullOrWhiteSpace(process.StatusReason)
            ? "No reason recorded."
            : process.StatusReason;

        return
            $"NexaRun alert: process '{process.Name}' is down.\r\n\r\n" +
            $"Status: {process.Status}\r\n" +
            $"Reason: {reason}\r\n" +
            $"Time (UTC): {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}\r\n" +
            $"Host: {Environment.MachineName}\r\n";
    }

    private async Task SendViaSesApi(NexaRunSettings settings, string subject, string body)
    {
        if (string.IsNullOrWhiteSpace(settings.AwsAccessKeyId)
            || string.IsNullOrWhiteSpace(settings.AwsSecretAccessKey))
            throw new InvalidOperationException("SES API requires AWS access key ID and secret access key.");

        var credentials = new BasicAWSCredentials(
            settings.AwsAccessKeyId.Trim(),
            settings.AwsSecretAccessKey);

        var regionName = string.IsNullOrWhiteSpace(settings.AwsRegion)
            ? "us-east-1"
            : settings.AwsRegion.Trim();

        using var client = new AmazonSimpleEmailServiceV2Client(credentials, RegionEndpoint.GetBySystemName(regionName));

        var response = await client.SendEmailAsync(new SendEmailRequest
        {
            FromEmailAddress = settings.AlertEmailFrom!.Trim(),
            Destination = new Destination { ToAddresses = [settings.AlertEmailTo!.Trim()] },
            Content = new EmailContent
            {
                Simple = new Message
                {
                    Subject = new Content { Data = subject, Charset = "UTF-8" },
                    Body = new Body { Text = new Content { Data = body, Charset = "UTF-8" } }
                }
            }
        });

        logger.LogDebug("SES API MessageId: {Id}", response.MessageId);
    }

    private async Task SendViaSesSmtp(NexaRunSettings settings, string subject, string body)
    {
        if (string.IsNullOrWhiteSpace(settings.SesSmtpUsername)
            || string.IsNullOrWhiteSpace(settings.SesSmtpPassword))
            throw new InvalidOperationException("SES SMTP requires SMTP username and password from the SES console.");

        var host = EmailAlertProvider.SesSmtpHost(settings.AwsRegion);
        var port = settings.SesSmtpPort is > 0 and <= 65535 ? settings.SesSmtpPort : 587;

        using var message = new MailMessage(settings.AlertEmailFrom!.Trim(), settings.AlertEmailTo!.Trim())
        {
            Subject = subject,
            Body = body,
            IsBodyHtml = false
        };

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = settings.SesSmtpUseTls,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Credentials = new NetworkCredential(
                settings.SesSmtpUsername.Trim(),
                settings.SesSmtpPassword)
        };

        await client.SendMailAsync(message);
    }
}
