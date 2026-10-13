namespace EstateFlow.Core.Dtos;

public class CorrespondenceDto
{
    public Guid CorrespondenceId { get; set; }
    public DateTime SentAtUtc { get; set; }
    public string Channel { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public string SentBy { get; set; } = string.Empty;
    public string RecipientName { get; set; } = string.Empty;
    public string? RecipientEmail { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid? DocumentId { get; set; }
}
