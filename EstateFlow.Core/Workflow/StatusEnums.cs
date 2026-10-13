namespace EstateFlow.Core.Workflow;

// How a diary entry reminds the controller.
public enum NotifyMethod
{
    Email,
    InApp,
    Both
}

// Status of a free-text progress (timeline) update.
public enum ProgressStatus
{
    Pending,
    InProgress,
    Complete
}

public static class StatusParsing
{
    public static bool TryParseNotifyMethod(string? value, out NotifyMethod method) =>
        Enum.TryParse(value?.Trim(), ignoreCase: true, out method) &&
        Enum.IsDefined(method);

    public static bool TryParseProgressStatus(string? value, out ProgressStatus status) =>
        Enum.TryParse(value?.Replace(" ", string.Empty).Trim(), ignoreCase: true, out status) &&
        Enum.IsDefined(status);
}
