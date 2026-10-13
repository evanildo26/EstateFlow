using System.IO;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using EstateFlow.Desktop.Services;
using QRCoder;

namespace EstateFlow.Desktop.Mvvm;

// "Security & 2FA": lets a staff member turn on two-factor authentication by
// scanning a QR code with an authenticator app (Microsoft or Google
// Authenticator, Authy, ...).
public class SecurityViewModel : BaseViewModel
{
    private readonly IApiClient _apiClient;
    private readonly IAuthState _authState;

    public SecurityViewModel(IApiClient apiClient, IAuthState authState)
    {
        _apiClient = apiClient;
        _authState = authState;

        StartSetupCommand = new AsyncRelayCommand(async _ => await StartSetupAsync(), _ => !IsBusy);
        EnableCommand = new AsyncRelayCommand(async _ => await EnableAsync(), _ => !IsBusy);
        CancelSetupCommand = new RelayCommand(_ => CancelSetup());
    }

    public string UserName => _authState.FullName ?? string.Empty;

    private bool _isEnabled;
    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (SetProperty(ref _isEnabled, value))
                OnPropertyChanged(nameof(IsDisabled));
        }
    }

    public bool IsDisabled => !IsEnabled && !IsSettingUp;

    private bool _isSettingUp;
    public bool IsSettingUp
    {
        get => _isSettingUp;
        set
        {
            if (SetProperty(ref _isSettingUp, value))
                OnPropertyChanged(nameof(IsDisabled));
        }
    }

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    private BitmapImage? _qrCode;
    public BitmapImage? QrCode
    {
        get => _qrCode;
        set => SetProperty(ref _qrCode, value);
    }

    // The secret in groups of four, for typing in by hand.
    private string _manualKey = string.Empty;
    public string ManualKey
    {
        get => _manualKey;
        set => SetProperty(ref _manualKey, value);
    }

    private string _code = string.Empty;
    public string Code
    {
        get => _code;
        set => SetProperty(ref _code, value);
    }

    private string _message = string.Empty;
    public string Message
    {
        get => _message;
        set => SetProperty(ref _message, value);
    }

    public ICommand StartSetupCommand { get; }
    public ICommand EnableCommand { get; }
    public ICommand CancelSetupCommand { get; }

    public async Task LoadAsync()
    {
        try
        {
            IsBusy = true;
            IsEnabled = (await _apiClient.GetTwoFactorStatusAsync()).IsEnabled;
            Message = string.Empty;
        }
        catch (Exception ex)
        {
            Message = $"Could not check your two-factor status: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task StartSetupAsync()
    {
        try
        {
            IsBusy = true;
            Message = string.Empty;

            var setup = await _apiClient.StartTwoFactorSetupAsync();

            QrCode = RenderQr(setup.AuthenticatorUri);
            ManualKey = string.Join(" ", Chunk(setup.SharedKey.ToUpperInvariant(), 4));
            Code = string.Empty;
            IsSettingUp = true;
        }
        catch (Exception ex)
        {
            Message = $"Could not start setup: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task EnableAsync()
    {
        var code = Code.Replace(" ", string.Empty);
        if (code.Length != 6 || !code.All(char.IsDigit))
        {
            Message = "Enter the 6-digit code shown in your authenticator app.";
            return;
        }

        try
        {
            IsBusy = true;

            await _apiClient.EnableTwoFactorAsync(code);

            IsSettingUp = false;
            IsEnabled = true;
            QrCode = null;
            ManualKey = string.Empty;
            Code = string.Empty;
            Message = "Two-factor authentication is on. You'll be asked for a code each time you sign in.";
        }
        catch (Exception ex)
        {
            Message = $"{ex.Message} Check that your phone's clock is correct and try the newest code.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void CancelSetup()
    {
        IsSettingUp = false;
        QrCode = null;
        ManualKey = string.Empty;
        Code = string.Empty;
        Message = string.Empty;
    }

    private static BitmapImage RenderQr(string text)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
        var png = new PngByteQRCode(data).GetGraphic(8);

        var image = new BitmapImage();
        using (var stream = new MemoryStream(png))
        {
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = stream;
            image.EndInit();
        }

        image.Freeze();
        return image;
    }

    private static IEnumerable<string> Chunk(string value, int size)
    {
        for (var i = 0; i < value.Length; i += size)
            yield return value.Substring(i, Math.Min(size, value.Length - i));
    }
}
