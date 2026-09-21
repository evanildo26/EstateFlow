namespace EstateFlow.Core.Models;

public class Liability
{
    public Guid LiabilityId { get; set; } = Guid.NewGuid();
    public Guid EstateId { get; set; }
    public Estate? Estate { get; set; }

    public string CreditorName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Amount { get; set; }
}