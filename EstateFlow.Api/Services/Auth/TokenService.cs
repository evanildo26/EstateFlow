using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EstateFlow.Core.Models;
using Microsoft.IdentityModel.Tokens;

namespace EstateFlow.Api.Services.Auth;

public class TokenService
{
    // Audience for the short-lived token issued after a correct password when
    // 2FA is on. It differs from Jwt:Audience, so the JWT bearer middleware
    // rejects it as an access token — it can only be exchanged at verify-2fa.
    private const string TwoFactorAudience = "EstateFlow.TwoFactorChallenge";
    private static readonly TimeSpan TwoFactorChallengeLifetime = TimeSpan.FromMinutes(5);

    private readonly IConfiguration _configuration;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateToken(ApplicationUser user, IList<string> roles)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Name, user.FullName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (user.ControllerId.HasValue)
            claims.Add(new Claim("controllerId", user.ControllerId.Value.ToString()));

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var expiryMinutes = double.Parse(_configuration["Jwt:ExpiryMinutes"] ?? "480");

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: GetSigningCredentials());

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // Proof that the password step succeeded. Bound to the user id and the
    // user's security stamp, so it is invalidated by a password or 2FA reset.
    public string GenerateTwoFactorChallengeToken(ApplicationUser user)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new("stamp", user.SecurityStamp ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: TwoFactorAudience,
            claims: claims,
            expires: DateTime.UtcNow.Add(TwoFactorChallengeLifetime),
            signingCredentials: GetSigningCredentials());

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // Returns (userId, securityStamp) when the challenge token is valid, otherwise null.
    public (string UserId, string SecurityStamp)? ValidateTwoFactorChallengeToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = TwoFactorAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = GetSigningKey()
        };

        try
        {
            var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
            var principal = handler.ValidateToken(token, parameters, out _);

            var userId = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            var stamp = principal.FindFirst("stamp")?.Value;

            return userId is null || stamp is null
                ? null
                : (userId, stamp);
        }
        catch (SecurityTokenException)
        {
            return null;
        }
        catch (ArgumentException)
        {
            // Malformed token string.
            return null;
        }
    }

    private SymmetricSecurityKey GetSigningKey() =>
        new(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));

    private SigningCredentials GetSigningCredentials() =>
        new(GetSigningKey(), SecurityAlgorithms.HmacSha256);
}
