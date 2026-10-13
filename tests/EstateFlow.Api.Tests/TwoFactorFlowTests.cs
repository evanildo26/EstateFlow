using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using EstateFlow.Core.Dtos;

namespace EstateFlow.Api.Tests;

public class TwoFactorFlowTests : IClassFixture<EstateFlowApiFactory>
{
    private readonly EstateFlowApiFactory _factory;

    public TwoFactorFlowTests(EstateFlowApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Full_two_factor_sign_in_works_and_challenge_token_is_not_an_access_token()
    {
        var email = $"2fa-{Guid.NewGuid():N}@test.local";
        const string password = "TwoFactorPassw0rd";
        var signedIn = await _factory.CreateControllerClientAsync(email, password);

        // Enable 2FA the way the (future) setup screen will.
        var setup = await signedIn.PostAsync("api/auth/2fa/setup", null);
        var setupBody = await setup.Content.ReadFromJsonAsync<TwoFactorSetupResponse>();
        var enable = await signedIn.PostAsJsonAsync("api/auth/2fa/enable",
            new EnableTwoFactorRequest { Code = Totp(setupBody!.SharedKey) });
        Assert.Equal(HttpStatusCode.OK, enable.StatusCode);

        // Re-running setup must not silently replace the working key.
        var resetup = await signedIn.PostAsync("api/auth/2fa/setup", null);
        Assert.Equal(HttpStatusCode.Conflict, resetup.StatusCode);

        // Password step: no access token yet, only a challenge token.
        var client = _factory.CreateClient();
        var login = await client.PostAsJsonAsync("api/auth/login", new LoginRequest { Email = email, Password = password });
        var loginBody = await login.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.True(loginBody!.RequiresTwoFactor);
        Assert.Null(loginBody.Token);
        Assert.False(string.IsNullOrEmpty(loginBody.TwoFactorToken));

        // The challenge token cannot be used to call the API.
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginBody.TwoFactorToken);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("api/estates")).StatusCode);
        client.DefaultRequestHeaders.Authorization = null;

        // Wrong code is refused.
        var wrong = await client.PostAsJsonAsync("api/auth/login/verify-2fa",
            new VerifyTwoFactorRequest { TwoFactorToken = loginBody.TwoFactorToken!, Code = "000000" });
        Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);

        // Correct code completes sign-in.
        var verify = await client.PostAsJsonAsync("api/auth/login/verify-2fa",
            new VerifyTwoFactorRequest { TwoFactorToken = loginBody.TwoFactorToken!, Code = Totp(setupBody.SharedKey) });
        Assert.Equal(HttpStatusCode.OK, verify.StatusCode);

        var verifyBody = await verify.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", verifyBody!.Token);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("api/estates")).StatusCode);
    }

    // RFC 6238 TOTP (SHA-1, 30 s, 6 digits), as used by authenticator apps.
    private static string Totp(string base32Key)
    {
        var key = Base32Decode(base32Key);
        var counter = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30;
        var counterBytes = BitConverter.GetBytes(counter);
        if (BitConverter.IsLittleEndian) Array.Reverse(counterBytes);

        var hash = HMACSHA1.HashData(key, counterBytes);
        var offset = hash[^1] & 0x0F;
        var binary = ((hash[offset] & 0x7F) << 24) | (hash[offset + 1] << 16) | (hash[offset + 2] << 8) | hash[offset + 3];

        return (binary % 1_000_000).ToString("D6");
    }

    private static byte[] Base32Decode(string input)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var bits = 0;
        var value = 0;
        var output = new List<byte>();

        foreach (var c in input.TrimEnd('=').ToUpperInvariant())
        {
            value = (value << 5) | alphabet.IndexOf(c);
            bits += 5;
            if (bits >= 8)
            {
                output.Add((byte)(value >> (bits - 8)));
                bits -= 8;
            }
        }

        return output.ToArray();
    }
}
