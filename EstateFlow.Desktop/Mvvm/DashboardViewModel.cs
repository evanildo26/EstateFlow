using System.Collections.ObjectModel;
using System.Windows.Input;
using EstateFlow.Core.Dtos;
using EstateFlow.Desktop.Services;

namespace EstateFlow.Desktop.Mvvm;

public class DashboardViewModel : BaseViewModel
{
    private readonly IApiClient _apiClient;
    private readonly INavigationService _navigation;

    public ObservableCollection<EstateSummaryDto> Estates { get; } = new();

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

    public ICommand LoadEstatesCommand { get; }
    public ICommand OpenEstateCommand { get; }
    public ICommand NewEstateCommand { get; }

    public DashboardViewModel(IApiClient apiClient, INavigationService navigation)
    {
        _apiClient = apiClient;
        _navigation = navigation;

        LoadEstatesCommand = new AsyncRelayCommand(async _ => await LoadEstatesAsync());
        OpenEstateCommand = new RelayCommand(ExecuteOpenEstate);
        NewEstateCommand = new RelayCommand(_ => _navigation.NavigateToNewEstate());

        _ = LoadEstatesAsync();
    }

    private void ExecuteOpenEstate(object? parameter)
    {
        if (parameter is Guid estateId)
            _navigation.NavigateToEstateDetail(estateId);
    }

    private async Task LoadEstatesAsync()
    {
        IsLoading = true;
        StatusMessage = "Loading estates...";
        try
        {
            var estates = await _apiClient.GetEstatesAsync();
            Estates.Clear();
            foreach (var estate in estates)
                Estates.Add(estate);

            StatusMessage = Estates.Count == 0 ? "No estates found." : string.Empty;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Could not load estates: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}