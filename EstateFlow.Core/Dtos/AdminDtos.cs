namespace EstateFlow.Core.Dtos;

public class StaffMemberDto
{
    public string UserId { get; set; } = string.Empty;
    public Guid? ControllerId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool TwoFactorEnabled { get; set; }
    public bool IsLockedOut { get; set; }
    public bool IsDeactivated { get; set; }
    public int AssignedEstateCount { get; set; }
}

public class ResetPasswordRequest
{
    public string NewPassword { get; set; } = string.Empty;
}

public class AdminEstateDto
{
    public Guid EstateId { get; set; }
    public string FileNumber { get; set; } = string.Empty;
    public string? DeceasedFullName { get; set; }
    public string EstateStatus { get; set; } = string.Empty;
    public Guid ControllerId { get; set; }
    public string? ControllerName { get; set; }
    public DateTime? FinalisedOn { get; set; }
}

public class ReassignEstateRequest
{
    public Guid ControllerId { get; set; }
}

public class AuditEntryDto
{
    public DateTime TimestampUtc { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public Guid? EstateId { get; set; }
    public string? FileNumber { get; set; }
    public string Target { get; set; } = string.Empty;
    public int StatusCode { get; set; }
}

public class LetterTemplateDto
{
    public string LetterType { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsCustomised { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public string? UpdatedBy { get; set; }
    public List<string> Placeholders { get; set; } = new();
}

public class UpdateLetterTemplateRequest
{
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
}

public class TwoFactorStatusDto
{
    public bool IsEnabled { get; set; }
}
