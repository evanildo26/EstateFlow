namespace EstateFlow.Core.Workflow;

// The overall estate status values shown in lists and reports.
public static class EstateStatuses
{
    public const string Reported = "Reported";
    public const string InProgress = "In Progress";
    public const string Finalised = "Finalised";

    public static readonly string[] All = { Reported, InProgress, Finalised };

    // Older builds of the edit form saved values such as
    // "System.Windows.Controls.ComboBoxItem: In Progress". Clean those up.
    public static string Normalize(string? value)
    {
        var text = value?.Trim() ?? string.Empty;
        var colon = text.LastIndexOf(':');
        if (colon >= 0)
            text = text[(colon + 1)..].Trim();

        var match = All.FirstOrDefault(s => string.Equals(s, text, StringComparison.OrdinalIgnoreCase));
        if (match is not null)
            return match;

        return string.Equals(text, "InProgress", StringComparison.OrdinalIgnoreCase)
            ? InProgress
            : Reported;
    }
}
