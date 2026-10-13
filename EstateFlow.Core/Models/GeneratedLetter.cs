namespace EstateFlow.Core.Models;

public class GeneratedLetter
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } =
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
    public byte[] Data { get; set; } = Array.Empty<byte>();

    public string LetterType { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string RecipientName { get; set; } = string.Empty;
}
