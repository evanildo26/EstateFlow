namespace EstateFlow.Functions.Services;

// POPIA: email addresses of heirs and executors are personal information and
// must not end up in Application Insights. Logs keep enough to troubleshoot
// ("j***@gmail.com") without storing the full address.
public static class LogRedaction
{
    public static string MaskEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return "(none)";

        var trimmed = email.Trim();
        var at = trimmed.IndexOf('@');

        if (at <= 0)
            return "***";

        return $"{trimmed[0]}***{trimmed[at..]}";
    }
}
