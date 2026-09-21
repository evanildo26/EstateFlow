namespace EstateFlow.Core.Models;

public class Beneficiary
{
    public Guid BeneficiaryId { get; set; } = Guid.NewGuid();

    public Guid EstateId { get; set; }

    public Estate? Estate { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string IdNumber { get; set; } = string.Empty;

    public string Relationship { get; set; } = string.Empty;

    public string HeirType { get; set; } = string.Empty;

    // Used only for estate communication / automated monthly reports.
    // Other unnecessary contact fields remain excluded.
    public string? Email { get; set; }
}
