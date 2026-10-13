namespace EstateFlow.Core.Models;

// A record of every email or letter sent from EstateFlow for an estate:
// who sent it, to whom, when, and what it was about. A law firm needs this
// history on the file; Azure logs are not a substitute.
public class CorrespondenceRecord
{
    public Guid CorrespondenceId { get; set; } = Guid.NewGuid();
    public Guid EstateId { get; set; }
    public Estate? Estate { get; set; }

    public DateTime SentAtUtc { get; set; } = DateTime.UtcNow;

    // "Email" or "Letter"
    public string Channel { get; set; } = "Email";

    // e.g. "Diary email", "Monthly report", "Vehicle transfer letter"
    public string Kind { get; set; } = string.Empty;

    public string SentBy { get; set; } = string.Empty;
    public string RecipientName { get; set; } = string.Empty;
    public string? RecipientEmail { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string? Summary { get; set; }

    // "Sent" or "Failed"
    public string Status { get; set; } = "Sent";

    // Set when the correspondence produced a stored document (e.g. a letter).
    public Guid? DocumentId { get; set; }
}
