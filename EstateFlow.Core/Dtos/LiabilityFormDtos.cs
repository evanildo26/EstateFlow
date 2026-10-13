namespace EstateFlow.Core.Dtos;

public class CreateLiabilityRequest
{
    public string CreditorName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Amount { get; set; }
}
