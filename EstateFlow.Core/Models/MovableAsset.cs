namespace EstateFlow.Core.Models;

public class MovableAsset
{
    public Guid AssetId { get; set; } = Guid.NewGuid();
    public Guid EstateId { get; set; }
    public Estate? Estate { get; set; }

    public string Description { get; set; } = string.Empty;
    public string AssetType { get; set; } = string.Empty;
    public decimal Value { get; set; }

    public bool IsVehicle { get; set; }
    public string? RegistrationNumber { get; set; }
    public string? VinNumber { get; set; }
    public string? Make { get; set; }

    public bool RequiresTrafficDeptLetter() => IsVehicle;
}