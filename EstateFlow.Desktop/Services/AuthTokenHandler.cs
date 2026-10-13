using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace EstateFlow.Desktop.Services;

public class AuthTokenHandler : DelegatingHandler
{
    private readonly IAuthState _authState;

    public AuthTokenHandler(IAuthState authState)
    {
        _authState = authState;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var hadToken = !string.IsNullOrEmpty(_authState.Token);

        if (hadToken)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _authState.Token);

        var response = await base.SendAsync(request, cancellationToken);

        // The token expired (after a working day) or the account was
        // deactivated: send the user back to sign in rather than letting
        // every screen fail with errors.
        if (hadToken &&
            response.StatusCode == HttpStatusCode.Unauthorized &&
            request.RequestUri?.AbsolutePath.Contains("/api/auth/", StringComparison.OrdinalIgnoreCase) != true)
        {
            ErrorReporter.SessionExpired?.Invoke();
        }

        return response;
    }
}
