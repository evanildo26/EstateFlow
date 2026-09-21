namespace EstateFlow.Core.Dtos;

public class EstateDto
{
    public Guid EstateId { get; set; }
    public string FileNumber { get; set; } = string.Empty;
    public string? MastersReference { get; set; }
    public DateTime DateOfDeath { get; set; }
    public string EstateStatus { get; set; } = string.Empty;
    public bool IsDutiable { get; set; }
    public Guid ControllerId { get; set; }

    public EstateControllerDto? Controller { get; set; }
    public DeceasedDto? Deceased { get; set; }
    public List<ExecutorDto> Executors { get; set; } = new();
    public List<BeneficiaryDto> Beneficiaries { get; set; } = new();
    public List<MovableAssetDto> MovableAssets { get; set; } = new();
    public List<ImmovablePropertyDto> ImmovableProperties { get; set; } = new();
    public List<FinancialClaimDto> FinancialClaims { get; set; } = new();
    public List<DiaryEntryDto> DiaryEntries { get; set; } = new();
    public List<ProgressStageDto> ProgressStages { get; set; } = new();
    public List<ScannedDocumentDto> ScannedDocuments { get; set; } = new();
}

public class EstateControllerDto
{
    public Guid ControllerId { get; set; }
    public string StaffName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class DeceasedDto
{
    public Guid DeceasedId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string IdNumber { get; set; } = string.Empty;
    public string? MaritalStatus { get; set; }
}

public class ExecutorDto
{
    public Guid ExecutorId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string IdNumber { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? Email { get; set; }
}

public class BeneficiaryDto
{
    public Guid BeneficiaryId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string IdNumber { get; set; } = string.Empty;
    public string Relationship { get; set; } = string.Empty;
    public string HeirType { get; set; } = string.Empty;
    public string? Email { get; set; }
}

public class MovableAssetDto
{
    public Guid AssetId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string AssetType { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public bool IsVehicle { get; set; }
    public string? RegistrationNumber { get; set; }
    public string? VinNumber { get; set; }
    public string? Make { get; set; }
}

public class ImmovablePropertyDto
{
    public Guid AssetId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? TitleDeedNumber { get; set; }
    public decimal MunicipalValue { get; set; }
}

public class FinancialClaimDto
{
    public Guid ClaimId { get; set; }
    public string InstitutionName { get; set; } = string.Empty;
    public string InstitutionAddress { get; set; } = string.Empty;
    public string AccountReference { get; set; } = string.Empty;
    public decimal Value { get; set; }
}

public class DiaryEntryDto
{
    public Guid DiaryId { get; set; }
    public DateTime DueDate { get; set; }
    public string Note { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public string NotifyBy { get; set; } = string.Empty;
}

public class ProgressStageDto
{
    public Guid StageId { get; set; }
    public string StageName { get; set; } = string.Empty;
    public DateTime? TargetDate { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class ScannedDocumentDto
{
    public Guid DocumentId { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public DateTime UploadDate { get; set; }
}