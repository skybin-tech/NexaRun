using System.Text.RegularExpressions;
using NexaRun.Shared.Models;

namespace NexaRun.Shared;

public static partial class ProcessUrlHelper
{
    [GeneratedRegex(@"-p\s+(\d+)", RegexOptions.IgnoreCase)]
    private static partial Regex PortArgumentRegex();

    public static string? DeriveLocalUrl(string? arguments, IDictionary<string, string>? environment = null)
    {
        var match = PortArgumentRegex().Match(arguments ?? string.Empty);
        if (match.Success)
            return $"http://localhost:{match.Groups[1].Value}";

        if (environment != null &&
            environment.TryGetValue("PORT", out var port) &&
            !string.IsNullOrWhiteSpace(port))
        {
            return $"http://localhost:{port.Trim()}";
        }

        return null;
    }

    public static string? ResolveUrl(string? url, string? arguments, IDictionary<string, string>? environment = null)
    {
        if (!string.IsNullOrWhiteSpace(url))
            return url.Trim();

        return DeriveLocalUrl(arguments, environment);
    }

    public static void ApplyUrl(NexaProcess process) =>
        process.Url = ResolveUrl(process.Url, process.Arguments, process.Environment);

    public static void ApplyUrl(StartOptions options) =>
        options.Url = ResolveUrl(options.Url, options.Arguments, options.Environment);
}
