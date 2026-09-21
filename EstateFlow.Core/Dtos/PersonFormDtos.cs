namespace EstateFlow.Core.Dtos;

public class CreateExecutorRequest
{
    public string FullName { get; set; } = string.Empty;

    public string IdNumber { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;
}

public class CreateBeneficiaryRequest
{
    public string FullName { get; set; } = string.Empty;

    public string IdNumber { get; set; } = string.Empty;

    public string Relationship { get; set; } = string.Empty;

    public string HeirType { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;
}
