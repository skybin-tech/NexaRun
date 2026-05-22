namespace NexaRun.Shared.Config;

public static class EmailAlertProviders
{
    public const string SesApi = "ses-api";
    public const string SesSmtp = "ses-smtp";

    public static bool IsValid(string? value) =>
        string.Equals(value, SesApi, StringComparison.OrdinalIgnoreCase)
        || string.Equals(value, SesSmtp, StringComparison.OrdinalIgnoreCase);

    public static string Normalize(string? value) =>
        string.Equals(value, SesSmtp, StringComparison.OrdinalIgnoreCase) ? SesSmtp : SesApi;
}
