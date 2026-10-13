namespace EstateFlow.Desktop.Services;

public class AuthState : IAuthState
{
    public string? Token { get; set; }
    public string? FullName { get; set; }
    public string? Role { get; set; }
    public bool IsAuthenticated => !string.IsNullOrEmpty(Token);

    public bool IsAdministrator => Role == "FirmAdministrator";

    public string RoleLabel => Role switch
    {
        "FirmAdministrator" => "Firm Administrator",
        "EstateController" => "Estate Controller",
        null or "" => string.Empty,
        _ => Role
    };

    public void Clear()
    {
        Token = null;
        FullName = null;
        Role = null;
    }
}
