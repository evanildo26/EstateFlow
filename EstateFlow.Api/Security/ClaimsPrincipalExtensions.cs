using System.Security.Claims;

namespace EstateFlow.Api.Security;

public static class ClaimsPrincipalExtensions
{
    public const string ControllerIdClaim = "controllerId";

    // The staff record (EstateController) linked to this login, if any.
    public static bool TryGetControllerId(
        this ClaimsPrincipal principal,
        out Guid controllerId)
    {
        controllerId = Guid.Empty;

        var value = principal.FindFirst(ControllerIdClaim)?.Value;

        return value is not null &&
               Guid.TryParse(value, out controllerId);
    }
}
