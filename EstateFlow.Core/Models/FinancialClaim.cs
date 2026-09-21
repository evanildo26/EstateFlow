namespace EstateFlow.Core.Models;

public class FinancialClaim
{
    public Guid ClaimId { get; set; } = Guid.NewGuid();
    public Guid EstateId { get; set; }
    public Estate? Estate { get; set; }

    public string InstitutionName { get; set; } = string.Empty;
    public string InstitutionAddress { get; set; } = string.Empty;
    public string AccountReference { get; set; } = string.Empty;
    public decimal Value { get; set; }

    public bool RequiresInstitutionLetter() => true;
}