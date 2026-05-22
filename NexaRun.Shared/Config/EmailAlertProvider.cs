namespace NexaRun.Shared.Config;

public static class EmailAlertProvider
{
    public const string SesApi = "ses-api";
    public const string SesSmtp = "ses-smtp";

    public static string Normalize(string? value) =>
        value?.Trim().ToLowerInvariant() switch
        {
            SesSmtp => SesSmtp,
            _ => SesApi
        };

    public static string SesSmtpHost(string? region)
    {
        var r = string.IsNullOrWhiteSpace(region) ? "us-east-1" : region.Trim();
        return $"email-smtp.{r}.amazonaws.com";
    }
}
