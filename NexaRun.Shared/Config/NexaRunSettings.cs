namespace NexaRun.Shared.Config;

public class NexaRunSettings
{
    /// <summary>Periodically try one start for each Errored process.</summary>
    public bool FailedRecoveryEnabled { get; set; } = true;

    /// <summary>Minutes between failed-process recovery checks (minimum 10).</summary>
    public int FailedRecoveryIntervalMinutes { get; set; } = 10;

    /// <summary>Send email when a process transitions from Online to Errored.</summary>
    public bool EmailAlertEnabled { get; set; }

    /// <summary>ses-api (SES API) or ses-smtp (SES SMTP interface).</summary>
    public string EmailProvider { get; set; } = EmailAlertProvider.SesApi;

    public string? AlertEmailTo { get; set; }

    /// <summary>Verified SES sender (From).</summary>
    public string? AlertEmailFrom { get; set; }

    /// <summary>AWS region for SES (e.g. us-east-1).</summary>
    public string AwsRegion { get; set; } = "us-east-1";

    /// <summary>SES API — IAM access key ID.</summary>
    public string? AwsAccessKeyId { get; set; }

    /// <summary>SES API — IAM secret access key.</summary>
    public string? AwsSecretAccessKey { get; set; }

    /// <summary>SES SMTP — SMTP username from SES console.</summary>
    public string? SesSmtpUsername { get; set; }

    /// <summary>SES SMTP — SMTP password from SES console.</summary>
    public string? SesSmtpPassword { get; set; }

    public int SesSmtpPort { get; set; } = 587;

    public bool SesSmtpUseTls { get; set; } = true;
}
