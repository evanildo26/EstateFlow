using System.Windows.Input;
using EstateFlow.Core.Dtos;
using EstateFlow.Desktop.Services;

namespace EstateFlow.Desktop.Mvvm;

// The estate screen. Split into partial classes, one per area, matching the
// tab UserControls in Views/EstateTabs:
//   EstateDetailViewModel.People.cs         Executors, Heirs, portal invitations
//   EstateDetailViewModel.Assets.cs         Movable, Immovable, Claims, letters
//   EstateDetailViewModel.Liabilities.cs    Debts of the deceased
//   EstateDetailViewModel.Diary.cs          Diary entries
//   EstateDetailViewModel.Timeline.cs       Free-text progress updates
//   EstateDetailViewModel.Checklist.cs      Master's-process stages & deadlines
//   EstateDetailViewModel.Correspondence.cs Emails and letters sent
public partial class EstateDetailViewModel : BaseViewModel
{
    private readonly IApiClient _apiClient;
    private readonly INavigationService _navigation;
    private Guid _estateId;

    private EstateDto? _estate;
    public EstateDto? Estate
    {
        get => _estate;
        set
        {
            if (SetProperty(ref _estate, value))
            {
                OnPropertyChanged(nameof(CurrentProgressTitle));
                OnPropertyChanged(nameof(CurrentProgressDate));
                OnPropertyChanged(nameof(CurrentProgressStatus));
                OnPropertyChanged(nameof(CurrentProgressSummary));

                RebuildChecklist();
            }
        }
    }

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    private string _statusMessage = string.Empty;
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    // ─────────────────────────────────────────────
    // Commands
    // ─────────────────────────────────────────────

    public ICommand BackCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DeleteEstateCommand { get; }

    public EstateDetailViewModel(
        IApiClient apiClient,
        INavigationService navigation)
    {
        _apiClient = apiClient;
        _navigation = navigation;

        BackCommand = new RelayCommand(
            _ => _navigation.NavigateToDashboard());

        EditCommand = new RelayCommand(_ =>
        {
            if (Estate is not null)
                _navigation.NavigateToEditEstate(Estate.EstateId);
        });

        DeleteEstateCommand =
            new AsyncRelayCommand(
                async _ => await ExecuteDeleteEstateAsync());

        InitializePeopleCommands();
        InitializeAssetCommands();
        InitializeLiabilityCommands();
        InitializeDiaryCommands();
        InitializeTimelineCommands();
        InitializeChecklistCommands();
        InitializeCorrespondenceCommands();
    }

    // ─────────────────────────────────────────────
    // Load / Refresh Estate
    // ─────────────────────────────────────────────

    public async Task LoadAsync(Guid estateId)
    {
        _estateId = estateId;
        IsLoading = true;
        StatusMessage = "Loading estate...";

        try
        {
            Estate = await _apiClient.GetEstateAsync(estateId);

            StatusMessage =
                Estate is null
                    ? "Estate not found."
                    : string.Empty;
        }
        catch (Exception ex)
        {
            StatusMessage =
                $"Could not load estate: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }

        if (Estate is not null)
            await LoadCorrespondenceAsync();
    }

    private async Task RefreshEstateAsync()
    {
        var refreshedEstate =
            await _apiClient.GetEstateAsync(_estateId);

        if (refreshedEstate is not null)
            Estate = refreshedEstate;
    }

    // Shared pattern for the tabs: show progress, run the API call, refresh
    // the estate, and report success or the server's message.
    private async Task RunAsync(
        string busyMessage,
        Func<Task> work,
        string successMessage,
        string failurePrefix)
    {
        if (_estateId == Guid.Empty)
        {
            StatusMessage = "No estate is currently open.";
            return;
        }

        try
        {
            IsLoading = true;
            StatusMessage = busyMessage;

            await work();
            await RefreshEstateAsync();

            StatusMessage = successMessage;
        }
        catch (Exception ex)
        {
            StatusMessage = $"{failurePrefix}: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private static bool Confirm(string message, string title) =>
        System.Windows.MessageBox.Show(
            message,
            title,
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question) == System.Windows.MessageBoxResult.Yes;

    private static void OpenFile(string path) =>
        System.Diagnostics.Process.Start(
            new System.Diagnostics.ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true
            });

    // ─────────────────────────────────────────────
    // Delete Estate
    // ─────────────────────────────────────────────

    private async Task ExecuteDeleteEstateAsync()
    {
        if (_estateId == Guid.Empty || Estate is null)
        {
            StatusMessage = "No estate is currently open.";
            return;
        }

        var result = System.Windows.MessageBox.Show(
            $"Are you sure you want to permanently delete estate {Estate.FileNumber}?\n\n" +
            "This will remove the estate and its associated records. " +
            "This action cannot be undone.",
            "Delete Estate",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);

        if (result != System.Windows.MessageBoxResult.Yes)
            return;

        try
        {
            IsLoading = true;
            StatusMessage = "Deleting estate...";

            await _apiClient.DeleteEstateAsync(_estateId);

            System.Windows.MessageBox.Show(
                "Estate deleted successfully.",
                "EstateFlow",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);

            _navigation.NavigateToDashboard();
        }
        catch (Exception ex)
        {
            StatusMessage =
                $"Could not delete estate: {ex.Message}";

            System.Windows.MessageBox.Show(
                StatusMessage,
                "Delete Estate Failed",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }
}
