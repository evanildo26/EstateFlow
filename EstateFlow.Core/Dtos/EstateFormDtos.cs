namespace EstateFlow.Core.Dtos;

public class CreateEstateRequest
{
    public string FileNumber { get; set; } = string.Empty;
    public string? MastersReference { get; set; }
    public DateTime DateOfDeath { get; set; }
    public bool IsDutiable { get; set; }

    public string DeceasedFullName { get; set; } = string.Empty;
    public string DeceasedIdNumber { get; set; } = string.Empty;
    public string? DeceasedMaritalStatus { get; set; }
}

public class UpdateEstateRequest
{
    public string FileNumber { get; set; } = string.Empty;
    public string? MastersReference { get; set; }
    public DateTime DateOfDeath { get; set; }
    public bool IsDutiable { get; set; }
    public string EstateStatus { get; set; } = string.Empty;

    public string DeceasedFullName { get; set; } = string.Empty;
    public string DeceasedIdNumber { get; set; } = string.Empty;
    public string? DeceasedMaritalStatus { get; set; }
}