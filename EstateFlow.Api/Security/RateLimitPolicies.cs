namespace EstateFlow.Api.Security;

public static class RateLimitPolicies
{
    // Applied to login and 2FA verification. Per-account lockout stops
    // guessing one password; this stops spraying many accounts from one IP.
    // Generous enough for a whole office sharing one public IP.
    public const string Auth = "auth";

    public const int AuthPermitLimit = 20;
    public static readonly TimeSpan AuthWindow = TimeSpan.FromMinutes(1);
}
