namespace EstateFlow.Core.Dtos;

// What an executor or heir sees in the read-only portal. Deliberately
// excludes ID numbers, account references, values and internal notes.
public class PortalEstateDto
{
    public Guid EstateId { get; set; }
    public string FileNumber { get; set; } = string.Empty;
    public string DeceasedFullName { get; set; } = string.Empty;
    public string EstateStatus { get; set; } = string.Empty;
    public string YourRole { get; set; } = string.Empty;
    public string? ControllerName { get; set; }
    public string? ControllerEmail { get; set; }
    public int PercentComplete { get; set; }
    public string? NextStep { get; set; }
    public List<PortalMilestoneDto> Milestones { get; set; } = new();
    public List<PortalUpdateDto> RecentUpdates { get; set; } = new();
}

public class PortalMilestoneDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime? CompletedOn { get; set; }
    public bool IsNext { get; set; }
}

public class PortalUpdateDto
{
    public DateTime Date { get; set; }
    public string Text { get; set; } = string.Empty;
}

public class InvitePortalUserRequest
{
    // "Executor" or "Beneficiary"
    public string PersonType { get; set; } = string.Empty;
    public Guid PersonId { get; set; }
}

public class SetPortalPasswordRequest
{
    public string Email { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}
