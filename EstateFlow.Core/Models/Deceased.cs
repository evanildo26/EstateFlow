namespace EstateFlow.Core.Models;

public class Deceased
{
    public Guid DeceasedId { get; set; } = Guid.NewGuid();
    public Guid EstateId { get; set; }
    public Estate? Estate { get; set; }

    public string FullName { get; set; } = string.Empty;
    public string IdNumber { get; set; } = string.Empty;
    public string? MaritalStatus { get; set; }
    // Note: date of death lives on Estate; residency/address fields
    // deliberately omitted here per client requirement F/G (see doc pain point G)
}