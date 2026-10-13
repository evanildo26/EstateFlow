namespace EstateFlow.Core.Models;

// POPIA audit trail: who viewed or changed which estate, and when.
public class AuditEntry
{
    public long AuditEntryId { get; set; }
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;

    public string? UserId { get; set; }
    public string UserName { get; set; } = string.Empty;

    // View | Create | Update | Delete | Download | Send | Admin
    public string Action { get; set; } = string.Empty;

    public Guid? EstateId { get; set; }

    // The API route that was called, e.g. "PUT api/Estates/{estateId}".
    public string Target { get; set; } = string.Empty;

    public int StatusCode { get; set; }
}
