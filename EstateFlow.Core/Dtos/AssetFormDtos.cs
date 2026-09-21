namespace EstateFlow.Core.Dtos;

public class CreateMovableAssetRequest
{
    public string Description { get; set; } = string.Empty;
    public string AssetType { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public bool IsVehicle { get; set; }
    public string? RegistrationNumber { get; set; }
    public string? VinNumber { get; set; }
    public string? Make { get; set; }
}

public class CreateImmovablePropertyRequest
{
    public string Description { get; set; } = string.Empty;
    public string? TitleDeedNumber { get; set; }
    public decimal MunicipalValue { get; set; }
}

public class CreateFinancialClaimRequest
{
    public string InstitutionName { get; set; } = string.Empty;
    public string InstitutionAddress { get; set; } = string.Empty;
    public string AccountReference { get; set; } = string.Empty;
    public decimal Value { get; set; }
}