using System.Windows.Input;
using EstateFlow.Core.Dtos;
using EstateFlow.Core.Validation;
using EstateFlow.Core.Workflow;
using EstateFlow.Desktop.Services;

namespace EstateFlow.Desktop.Mvvm;

public class EstateFormViewModel : BaseViewModel
{
    private readonly IApiClient _apiClient;
    private readonly INavigationService _navigation;

    private Guid? _editingEstateId;

    private string _fileNumber = string.Empty;
    public string FileNumber
    {
        get => _fileNumber;
        set => SetProperty(ref _fileNumber, value);
    }

    private string _mastersReference = string.Empty;
    public string MastersReference
    {
        get => _mastersReference;
        set => SetProperty(ref _mastersReference, value);
    }

    private DateTime _dateOfDeath = DateTime.Today;
    public DateTime DateOfDeath
    {
        get => _dateOfDeath;
        set
        {
            if (SetProperty(ref _dateOfDeath, value))
                OnPropertyChanged(nameof(DeceasedIdHint));
        }
    }

    private bool _isDutiable;
    public bool IsDutiable
    {
        get => _isDutiable;
        set => SetProperty(ref _isDutiable, value);
    }

    private string _estateStatus = "Reported";
    public string EstateStatus
    {
        get => _estateStatus;
        set => SetProperty(ref _estateStatus, value);
    }

    private string _deceasedFullName = string.Empty;
    public string DeceasedFullName
    {
        get => _deceasedFullName;
        set => SetProperty(ref _deceasedFullName, value);
    }

    private string _deceasedIdNumber = string.Empty;
    public string DeceasedIdNumber
    {
        get => _deceasedIdNumber;
        set
        {
            if (SetProperty(ref _deceasedIdNumber, value))
                OnPropertyChanged(nameof(DeceasedIdHint));
        }
    }

    // Live check so a typo is caught before it reaches a letter to a bank.
    public string DeceasedIdHint
    {
        get
        {
            var check = SouthAfricanIdNumber.Check(DeceasedIdNumber);

            if (!check.IsAcceptable)
                return check.Error!;

            return check.DateOfBirth is DateTime born && born >= DateOfDeath.Date
                ? "The date of birth in this ID number is on or after the date of death."
                : string.Empty;
        }
    }

    public IReadOnlyList<string> StatusOptions { get; } = EstateStatuses.All;

    private string _deceasedMaritalStatus = string.Empty;
    public string DeceasedMaritalStatus
    {
        get => _deceasedMaritalStatus;
        set => SetProperty(ref _deceasedMaritalStatus, value);
    }

    private string _headerText = "New Estate";
    public string HeaderText
    {
        get => _headerText;
        set => SetProperty(ref _headerText, value);
    }

    private string _statusMessage = string.Empty;
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public bool IsEditMode => _editingEstateId.HasValue;

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    public EstateFormViewModel(IApiClient apiClient, INavigationService navigation)
    {
        _apiClient = apiClient;
        _navigation = navigation;
        SaveCommand = new AsyncRelayCommand(async _ => await ExecuteSaveAsync());
        CancelCommand = new RelayCommand(_ => ExecuteCancel());
    }

    public void InitializeForCreate()
    {
        _editingEstateId = null;
        HeaderText = "New Estate";
        FileNumber = string.Empty;
        MastersReference = string.Empty;
        DateOfDeath = DateTime.Today;
        IsDutiable = false;
        EstateStatus = "Reported";
        DeceasedFullName = string.Empty;
        DeceasedIdNumber = string.Empty;
        DeceasedMaritalStatus = string.Empty;
        StatusMessage = string.Empty;
    }

    public async Task InitializeForEditAsync(Guid estateId)
    {
        _editingEstateId = estateId;
        HeaderText = "Edit Estate";
        StatusMessage = "Loading...";

        var estate = await _apiClient.GetEstateAsync(estateId);
        if (estate is null)
        {
            StatusMessage = "Estate not found.";
            return;
        }

        FileNumber = estate.FileNumber;
        MastersReference = estate.MastersReference ?? string.Empty;
        DateOfDeath = estate.DateOfDeath;
        IsDutiable = estate.IsDutiable;
        EstateStatus = EstateStatuses.Normalize(estate.EstateStatus);
        DeceasedFullName = estate.Deceased?.FullName ?? string.Empty;
        DeceasedIdNumber = estate.Deceased?.IdNumber ?? string.Empty;
        DeceasedMaritalStatus = estate.Deceased?.MaritalStatus ?? string.Empty;
        StatusMessage = string.Empty;
    }

    private async Task ExecuteSaveAsync()
    {
        if (string.IsNullOrWhiteSpace(FileNumber) || string.IsNullOrWhiteSpace(DeceasedFullName))
        {
            StatusMessage = "File number and deceased full name are required.";
            return;
        }

        if (DeceasedIdHint.Length > 0)
        {
            StatusMessage = $"Deceased ID number: {DeceasedIdHint}";
            return;
        }

        try
        {
            if (IsEditMode)
            {
                var updateRequest = new UpdateEstateRequest
                {
                    FileNumber = FileNumber,
                    MastersReference = MastersReference,
                    DateOfDeath = DateOfDeath,
                    IsDutiable = IsDutiable,
                    EstateStatus = EstateStatus,
                    DeceasedFullName = DeceasedFullName,
                    DeceasedIdNumber = DeceasedIdNumber,
                    DeceasedMaritalStatus = DeceasedMaritalStatus
                };
                await _apiClient.UpdateEstateAsync(_editingEstateId!.Value, updateRequest);
                _navigation.NavigateToEstateDetail(_editingEstateId.Value);
            }
            else
            {
                var createRequest = new CreateEstateRequest
                {
                    FileNumber = FileNumber,
                    MastersReference = MastersReference,
                    DateOfDeath = DateOfDeath,
                    IsDutiable = IsDutiable,
                    DeceasedFullName = DeceasedFullName,
                    DeceasedIdNumber = DeceasedIdNumber,
                    DeceasedMaritalStatus = DeceasedMaritalStatus
                };
                var newEstateId = await _apiClient.CreateEstateAsync(createRequest);
                _navigation.NavigateToEstateDetail(newEstateId);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Could not save estate: {ex.Message}";
        }
    }

    private void ExecuteCancel()
    {
        if (IsEditMode)
            _navigation.NavigateToEstateDetail(_editingEstateId!.Value);
        else
            _navigation.NavigateToDashboard();
    }
}