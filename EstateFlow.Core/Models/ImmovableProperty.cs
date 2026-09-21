namespace EstateFlow.Core.Models;

public class ImmovableProperty
{
    public Guid AssetId { get; set; } = Guid.NewGuid();
    public Guid EstateId { get; set; }
    public Estate? Estate { get; set; }

    public string Description { get; set; } = string.Empty;
    public string? TitleDeedNumber { get; set; }
    public decimal MunicipalValue { get; set; }
}