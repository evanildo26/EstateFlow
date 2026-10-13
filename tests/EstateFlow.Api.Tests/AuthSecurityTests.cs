using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using EstateFlow.Core.Dtos;

namespace EstateFlow.Api.Tests;

public class AuthSecurityTests : IClassFixture<EstateFlowApiFactory>
{
    private readonly EstateFlowApiFactory _factory;

    public AuthSecurityTests(EstateFlowApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Anonymous_user_cannot_register_an_account()
    {
        var anonymous = _factory.CreateClient();

        var response = await anonymous.PostAsJsonAsync("api/auth/register", new RegisterRequest
        {
            Email = "self-signup@test.local",
            Password = "Passw0rd123",
            FullName = "Self Signup",
            Role = "FirmAdministrator"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Estate_controller_cannot_register_accounts()
    {
        var controller = await _factory.CreateControllerClientAsync($"ctl-{Guid.NewGuid():N}@test.local");

        var response = await controller.PostAsJsonAsync("api/auth/register", new RegisterRequest
        {
            Email = $"escalate-{Guid.NewGuid():N}@test.local",
            Password = "Passw0rd123",
            FullName = "Escalation",
            Role = "FirmAdministrator"
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Administrator_cannot_create_portal_roles_yet()
    {
        var admin = await _factory.CreateAuthenticatedClientAsync(
            EstateFlowApiFactory.AdminEmail, EstateFlowApiFactory.AdminPassword);

        var response = await admin.PostAsJsonAsync("api/auth/register", new RegisterRequest
        {
            Email = $"heir-{Guid.NewGuid():N}@test.local",
            Password = "Passw0rd123",
            FullName = "Heir",
            Role = "Heir"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Account_locks_after_five_wrong_passwords()
    {
        var email = $"lock-{Guid.NewGuid():N}@test.local";
        await _factory.CreateControllerClientAsync(email, "RightPassw0rd");
        var client = _factory.CreateClient();

        for (var i = 0; i < 5; i++)
        {
            await client.PostAsJsonAsync("api/auth/login", new LoginRequest { Email = email, Password = "wrong" });
        }

        // Even the correct password is refused while locked.
        var response = await client.PostAsJsonAsync("api/auth/login",
            new LoginRequest { Email = email, Password = "RightPassw0rd" });

        Assert.Equal((HttpStatusCode)423, response.StatusCode);
    }

    [Fact]
    public async Task Two_factor_verification_requires_the_password_step()
    {
        var client = _factory.CreateClient();

        // Previously an email address and a guessed code were enough.
        var response = await client.PostAsJsonAsync("api/auth/login/verify-2fa", new VerifyTwoFactorRequest
        {
            TwoFactorToken = "",
            Code = "123456"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Forged_token_signed_with_another_key_is_rejected()
    {
        var client = _factory.CreateClient();

        // Header/payload for {"alg":"HS256"} with a bogus signature.
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9." +
            "eyJzdWIiOiJ4IiwiY29udHJvbGxlcklkIjoiMDAwMDAwMDAtMDAwMC0wMDAwLTAwMDAtMDAwMDAwMDAwMDAwIn0." +
            "c2lnbmF0dXJlLWRvZXMtbm90LW1hdGNo");

        var response = await client.GetAsync("api/estates");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
