using EstateFlow.Desktop.Services;
using System.Net;
using System.Net.Http;
using System.Windows.Controls;
using System.Windows.Input;

namespace EstateFlow.Desktop.Mvvm;

public class LoginViewModel : BaseViewModel
{
    private readonly INavigationService _navigation;
    private readonly IApiClient _apiClient;
    private readonly IAuthState _authState;

    // Returned by the API after a correct password when 2FA is on.
    // Valid for a few minutes; sent back with the authenticator code.
    private string? _twoFactorToken;
    private DateTime _twoFactorTokenIssuedUtc;

    // Slightly shorter than the server-side lifetime (5 minutes).
    private static readonly TimeSpan TwoFactorTokenLifetime = TimeSpan.FromMinutes(4.5);

    private string _email = string.Empty;
    public string Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }

    private string _errorMessage = string.Empty;
    public string ErrorMessage
    {
        get => _errorMessage;
        set
        {
            if (SetProperty(ref _errorMessage, value))
                OnPropertyChanged(nameof(HasError));
        }
    }

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    private bool _requiresTwoFactor;
    public bool RequiresTwoFactor
    {
        get => _requiresTwoFactor;
        set => SetProperty(ref _requiresTwoFactor, value);
    }

    private string _twoFactorCode = string.Empty;
    public string TwoFactorCode
    {
        get => _twoFactorCode;
        set => SetProperty(ref _twoFactorCode, value);
    }

    public ICommand LoginCommand { get; }
    public ICommand VerifyTwoFactorCommand { get; }

    public LoginViewModel(INavigationService navigation, IApiClient apiClient, IAuthState authState)
    {
        _navigation = navigation;
        _apiClient = apiClient;
        _authState = authState;

        LoginCommand = new AsyncRelayCommand(async param => await ExecuteLoginAsync(param));
        VerifyTwoFactorCommand = new AsyncRelayCommand(async _ => await ExecuteVerifyTwoFactorAsync());
    }

    private async Task ExecuteLoginAsync(object? parameter)
    {
        var passwordBox = parameter as PasswordBox;
        var password = passwordBox?.Password ?? string.Empty;

        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(password))
        {
            ErrorMessage = "Please enter both an email address and password.";
            return;
        }

        try
        {
            var result = await _apiClient.LoginAsync(Email.Trim(), password);

            if (result.RequiresTwoFactor)
            {
                _twoFactorToken = result.TwoFactorToken;
                _twoFactorTokenIssuedUtc = DateTime.UtcNow;
                RequiresTwoFactor = true;
                ErrorMessage = string.Empty;
                return;
            }

            CompleteSignIn(result);
        }
        catch (HttpRequestException ex)
        {
            ErrorMessage = DescribeFailure(ex, "Invalid email or password.");
        }
        catch (TaskCanceledException)
        {
            ErrorMessage = "The EstateFlow server took too long to respond. Check your connection and try again.";
        }
        finally
        {
            passwordBox?.Clear();
        }
    }

    private async Task ExecuteVerifyTwoFactorAsync()
    {
        if (string.IsNullOrWhiteSpace(TwoFactorCode))
        {
            ErrorMessage = "Please enter your authentication code.";
            return;
        }

        if (string.IsNullOrEmpty(_twoFactorToken) ||
            DateTime.UtcNow - _twoFactorTokenIssuedUtc > TwoFactorTokenLifetime)
        {
            ReturnToPasswordStep("Your sign-in session has expired. Please sign in again.");
            return;
        }

        try
        {
            var result = await _apiClient.VerifyTwoFactorAsync(_twoFactorToken, TwoFactorCode.Trim());
            CompleteSignIn(result);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Locked)
        {
            ReturnToPasswordStep(DescribeFailure(ex, string.Empty));
        }
        catch (HttpRequestException ex)
        {
            ErrorMessage = DescribeFailure(ex, "Invalid authentication code.");
        }
        catch (TaskCanceledException)
        {
            ErrorMessage = "The EstateFlow server took too long to respond. Check your connection and try again.";
        }
    }

    private void CompleteSignIn(Core.Dtos.LoginResponse result)
    {
        _twoFactorToken = null;
        TwoFactorCode = string.Empty;
        ErrorMessage = string.Empty;

        _authState.Token = result.Token;
        _authState.FullName = result.FullName;
        _authState.Role = result.Role;
        _navigation.NavigateToDashboard();
    }

    private void ReturnToPasswordStep(string message)
    {
        _twoFactorToken = null;
        TwoFactorCode = string.Empty;
        RequiresTwoFactor = false;
        ErrorMessage = message;
    }

    private static string DescribeFailure(HttpRequestException ex, string unauthorizedMessage) =>
        ex.StatusCode switch
        {
            HttpStatusCode.Unauthorized => unauthorizedMessage,
            HttpStatusCode.Locked =>
                "This account is temporarily locked after too many failed attempts. Try again in 15 minutes.",
            HttpStatusCode.Forbidden =>
                "This account has been deactivated. Please contact your Firm Administrator.",
            HttpStatusCode.TooManyRequests =>
                "Too many sign-in attempts from this network. Please wait a minute and try again.",
            null =>
                "Could not reach the EstateFlow server. Check your internet connection and try again.",
            _ =>
                "The EstateFlow server could not complete the sign-in. Please try again shortly."
        };
}
