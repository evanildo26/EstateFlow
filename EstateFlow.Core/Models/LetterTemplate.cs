namespace EstateFlow.Core.Models;

// Editable body text for generated letters. A Firm Administrator can change
// the wording; placeholders such as {DeceasedName} are filled in per estate.
// When no row exists for a letter type, the built-in default is used.
public class LetterTemplate
{
    public string LetterType { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public string UpdatedBy { get; set; } = string.Empty;
}
