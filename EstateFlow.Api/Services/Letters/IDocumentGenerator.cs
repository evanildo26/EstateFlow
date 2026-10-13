using EstateFlow.Core.Models;

namespace EstateFlow.Api.Services.Letters;

// Produces the estate-specific parts of a letter. The wording comes from the
// (editable) template; the letterhead and layout from DocxLetterBuilder.
public interface IDocumentGenerator
{
    string LetterType { get; }

    LetterDraft Draft(Estate estate, object sourceRecord);
}

public class LetterDraft
{
    public string RecipientName { get; set; } = string.Empty;
    public List<string> RecipientLines { get; set; } = new();
    public string FileNameStem { get; set; } = string.Empty;
    public Dictionary<string, string> Values { get; set; } = new();
}
