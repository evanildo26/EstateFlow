using EstateFlow.Api.Security;
using EstateFlow.Api.Services.Auth;
using EstateFlow.Core.Dtos;
using EstateFlow.Core.Models;
using EstateFlow.Data.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EstateFlow.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    // Only internal staff accounts can be created until the Executor/Heir
    // portal exists. Those roles are seeded but have no screens yet.
    private static readonly string[] RegistrableRoles =
    {
        "EstateController",
        "FirmAdministrator"
    };

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly TokenService _tokenService;
    private readonly EstateFlowDbContext _context;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        TokenService tokenService,
        EstateFlowDbContext context)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _context = context;
    }

    // Staff accounts are created by a Firm Administrator only.
    // The first administrator is created from the Bootstrap:* settings
    // at start-up (see AdminBootstrapper).
    [Authorize(Roles = "FirmAdministrator")]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (!RegistrableRoles.Contains(request.Role))
        {
            return BadRequest(new
            {
                message = $"Role must be one of: {string.Join(", ", RegistrableRoles)}."
            });
        }

        if (string.IsNullOrWhiteSpace(request.FullName))
            return BadRequest(new { message = "Full name is required." });

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName.Trim()
        };

        // Create the staff record and the login together, so a rejected
        // password doesn't leave an orphaned EstateController row behind.
        await using var transaction = await _context.Database.BeginTransactionAsync();

        // Internal staff (Estate Controller / Firm Administrator) get a linked
        // EstateController record — this is what Estate.ControllerId points to,
        // and what "assigned estates" and audit logging are tied to.
        var controllerRecord = new EstateController
        {
            StaffName = user.FullName,
            Role = request.Role,
            Email = request.Email
        };
        _context.Controllers.Add(controllerRecord);
        await _context.SaveChangesAsync();
        user.ControllerId = controllerRecord.ControllerId;

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        var roleResult = await _userManager.AddToRoleAsync(user, request.Role);
        if (!roleResult.Succeeded)
            return BadRequest(new { errors = roleResult.Errors.Select(e => e.Description) });

        await transaction.CommitAsync();

        return Ok(new { message = "User registered.", userId = user.Id });
    }

    [EnableRateLimiting(RateLimitPolicies.Auth)]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
            return Unauthorized(new { message = "Invalid email or password." });

        if (await _userManager.IsLockedOutAsync(user))
            return AdminController.IsDeactivated(user) ? Deactivated() : LockedOut();

        var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!passwordValid)
        {
            await _userManager.AccessFailedAsync(user);

            return await _userManager.IsLockedOutAsync(user)
                ? LockedOut()
                : Unauthorized(new { message = "Invalid email or password." });
        }

        if (await _userManager.GetTwoFactorEnabledAsync(user))
        {
            // The failed-attempt counter is only reset once the 2FA code is
            // also correct, so the code can't be brute-forced either.
            return Ok(new LoginResponse
            {
                RequiresTwoFactor = true,
                TwoFactorToken = _tokenService.GenerateTwoFactorChallengeToken(user)
            });
        }

        await _userManager.ResetAccessFailedCountAsync(user);
        return Ok(await CreateLoginResponseAsync(user));
    }

    [EnableRateLimiting(RateLimitPolicies.Auth)]
    [HttpPost("login/verify-2fa")]
    public async Task<ActionResult<LoginResponse>> VerifyTwoFactor([FromBody] VerifyTwoFactorRequest request)
    {
        // The challenge token proves the password step succeeded in the last
        // few minutes. Without it, an email + guessed code would be enough.
        var challenge = _tokenService.ValidateTwoFactorChallengeToken(request.TwoFactorToken);
        if (challenge is null)
            return Unauthorized(new { message = "Your sign-in session has expired. Please sign in again." });

        var user = await _userManager.FindByIdAsync(challenge.Value.UserId);
        if (user is null || user.SecurityStamp != challenge.Value.SecurityStamp)
            return Unauthorized(new { message = "Your sign-in session has expired. Please sign in again." });

        if (await _userManager.IsLockedOutAsync(user))
            return LockedOut();

        var code = request.Code.Replace(" ", string.Empty).Replace("-", string.Empty);

        var codeValid = await _userManager.VerifyTwoFactorTokenAsync(
            user, _userManager.Options.Tokens.AuthenticatorTokenProvider, code);

        if (!codeValid)
        {
            await _userManager.AccessFailedAsync(user);

            return await _userManager.IsLockedOutAsync(user)
                ? LockedOut()
                : Unauthorized(new { message = "Invalid authentication code." });
        }

        await _userManager.ResetAccessFailedCountAsync(user);
        return Ok(await CreateLoginResponseAsync(user));
    }

    [Authorize]
    [HttpPost("2fa/setup")]
    public async Task<ActionResult<TwoFactorSetupResponse>> SetupTwoFactor()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Unauthorized();

        // Resetting the key while 2FA is on would silently break the user's
        // existing authenticator app and lock them out.
        if (await _userManager.GetTwoFactorEnabledAsync(user))
        {
            return Conflict(new
            {
                message = "Two-factor authentication is already enabled for this account."
            });
        }

        await _userManager.ResetAuthenticatorKeyAsync(user);
        var unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);

        var email = user.Email ?? user.UserName;
        var authenticatorUri =
            $"otpauth://totp/EstateFlow:{Uri.EscapeDataString(email!)}?secret={unformattedKey}&issuer={Uri.EscapeDataString("EstateFlow")}&digits=6";

        return Ok(new TwoFactorSetupResponse
        {
            SharedKey = unformattedKey!,
            AuthenticatorUri = authenticatorUri
        });
    }

    [Authorize]
    [HttpPost("2fa/enable")]
    public async Task<IActionResult> EnableTwoFactor([FromBody] EnableTwoFactorRequest request)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Unauthorized();

        var code = request.Code.Replace(" ", string.Empty).Replace("-", string.Empty);

        var codeValid = await _userManager.VerifyTwoFactorTokenAsync(
            user, _userManager.Options.Tokens.AuthenticatorTokenProvider, code);

        if (!codeValid)
            return BadRequest(new { message = "Invalid authentication code." });

        await _userManager.SetTwoFactorEnabledAsync(user, true);
        return Ok(new { message = "Two-factor authentication enabled." });
    }

    [Authorize]
    [HttpGet("2fa/status")]
    public async Task<ActionResult<TwoFactorStatusDto>> GetTwoFactorStatus()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Unauthorized();

        return Ok(new TwoFactorStatusDto
        {
            IsEnabled = await _userManager.GetTwoFactorEnabledAsync(user)
        });
    }

    // Portal users (executors/heirs) choose their password from the link in
    // their invitation email. Restricted to portal accounts: staff passwords
    // are reset by a Firm Administrator.
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    [HttpPost("portal/set-password")]
    public async Task<IActionResult> SetPortalPassword([FromBody] SetPortalPasswordRequest request)
    {
        const string invalidLink = "This link is invalid or has expired. Ask your estate controller to send a new invitation.";

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
            return BadRequest(new { message = invalidLink });

        var roles = await _userManager.GetRolesAsync(user);
        if (!roles.Any(r => r is "Executor" or "Heir") || roles.Any(r => RegistrableRoles.Contains(r)))
            return BadRequest(new { message = invalidLink });

        var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
        if (!result.Succeeded)
        {
            var invalidToken = result.Errors.Any(e => e.Code == "InvalidToken");
            return BadRequest(new
            {
                message = invalidToken
                    ? invalidLink
                    : string.Join(" ", result.Errors.Select(e => e.Description))
            });
        }

        await _userManager.SetLockoutEndDateAsync(user, null);
        await _userManager.ResetAccessFailedCountAsync(user);

        return Ok(new { message = "Password set. You can now sign in." });
    }

    private async Task<LoginResponse> CreateLoginResponseAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);

        return new LoginResponse
        {
            RequiresTwoFactor = false,
            Token = _tokenService.GenerateToken(user, roles),
            FullName = user.FullName,
            Role = roles.FirstOrDefault()
        };
    }

    private ObjectResult Deactivated() =>
        StatusCode(StatusCodes.Status403Forbidden, new
        {
            message = "This account has been deactivated. Please contact your Firm Administrator."
        });

    private ObjectResult LockedOut() =>
        StatusCode(StatusCodes.Status423Locked, new
        {
            message = "This account is temporarily locked after too many failed sign-in attempts. Try again in 15 minutes."
        });
}
