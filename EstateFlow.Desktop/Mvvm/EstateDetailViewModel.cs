using System.Windows.Input;
using EstateFlow.Core.Dtos;
using EstateFlow.Desktop.Services;

namespace EstateFlow.Desktop.Mvvm;

public class EstateDetailViewModel : BaseViewModel
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
            }
        }
    }

    private ProgressStageDto? CurrentProgressItem =>
        Estate?.ProgressStages?
            .OrderByDescending(x => x.TargetDate ?? DateTime.MinValue)
            .FirstOrDefault();

    public string CurrentProgressTitle =>
        CurrentProgressItem?.StageName
        ?? "No progress update recorded";

    public string CurrentProgressDate =>
        CurrentProgressItem?.TargetDate is DateTime date
            ? date.ToString("dd MMM yyyy")
            : "—";

    public string CurrentProgressStatus =>
        CurrentProgressItem?.Status switch
        {
            "InProgress" => "In Progress",
            "Complete" => "Complete",
            "Pending" => "Pending",
            _ => CurrentProgressItem?.Status ?? "Not Started"
        };

    public string CurrentProgressSummary =>
        CurrentProgressItem is null
            ? "Add the first progress update from the Progress tab."
            : $"{CurrentProgressDate} • {CurrentProgressStatus}";

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
    // Quick-add: Executor
    // ─────────────────────────────────────────────

    private string _newExecutorFullName = string.Empty;
    public string NewExecutorFullName
    {
        get => _newExecutorFullName;
        set => SetProperty(ref _newExecutorFullName, value);
    }

    private string _newExecutorIdNumber = string.Empty;
    public string NewExecutorIdNumber
    {
        get => _newExecutorIdNumber;
        set => SetProperty(ref _newExecutorIdNumber, value);
    }


    private string _newExecutorEmail = string.Empty;
    public string NewExecutorEmail
    {
        get => _newExecutorEmail;
        set => SetProperty(ref _newExecutorEmail, value);
    }

    private string _newExecutorRole = "Nominated Executor";
    public string NewExecutorRole
    {
        get => _newExecutorRole;
        set => SetProperty(ref _newExecutorRole, value);
    }

    // ─────────────────────────────────────────────
    // Quick-add: Beneficiary
    // ─────────────────────────────────────────────

    private string _newBeneficiaryFullName = string.Empty;
    public string NewBeneficiaryFullName
    {
        get => _newBeneficiaryFullName;
        set => SetProperty(ref _newBeneficiaryFullName, value);
    }

    private string _newBeneficiaryIdNumber = string.Empty;
    public string NewBeneficiaryIdNumber
    {
        get => _newBeneficiaryIdNumber;
        set => SetProperty(ref _newBeneficiaryIdNumber, value);
    }


    private string _newBeneficiaryEmail = string.Empty;
    public string NewBeneficiaryEmail
    {
        get => _newBeneficiaryEmail;
        set => SetProperty(ref _newBeneficiaryEmail, value);
    }

    private string _newBeneficiaryRelationship = string.Empty;
    public string NewBeneficiaryRelationship
    {
        get => _newBeneficiaryRelationship;
        set => SetProperty(ref _newBeneficiaryRelationship, value);
    }

    private string _newBeneficiaryHeirType = "Heir";
    public string NewBeneficiaryHeirType
    {
        get => _newBeneficiaryHeirType;
        set => SetProperty(ref _newBeneficiaryHeirType, value);
    }

    // ─────────────────────────────────────────────
    // Quick-add: Movable Asset
    // ─────────────────────────────────────────────

    private string _newAssetDescription = string.Empty;
    public string NewAssetDescription
    {
        get => _newAssetDescription;
        set => SetProperty(ref _newAssetDescription, value);
    }

    private string _newAssetType = string.Empty;
    public string NewAssetType
    {
        get => _newAssetType;
        set => SetProperty(ref _newAssetType, value);
    }

    private string _newAssetValue = "0";
    public string NewAssetValue
    {
        get => _newAssetValue;
        set => SetProperty(ref _newAssetValue, value);
    }

    private bool _newAssetIsVehicle;
    public bool NewAssetIsVehicle
    {
        get => _newAssetIsVehicle;
        set => SetProperty(ref _newAssetIsVehicle, value);
    }

    private string _newAssetRegistrationNumber = string.Empty;
    public string NewAssetRegistrationNumber
    {
        get => _newAssetRegistrationNumber;
        set => SetProperty(ref _newAssetRegistrationNumber, value);
    }

    private string _newAssetVinNumber = string.Empty;
    public string NewAssetVinNumber
    {
        get => _newAssetVinNumber;
        set => SetProperty(ref _newAssetVinNumber, value);
    }

    private string _newAssetMake = string.Empty;
    public string NewAssetMake
    {
        get => _newAssetMake;
        set => SetProperty(ref _newAssetMake, value);
    }

    // ─────────────────────────────────────────────
    // Quick-add: Immovable Property
    // ─────────────────────────────────────────────

    private string _newPropertyDescription = string.Empty;
    public string NewPropertyDescription
    {
        get => _newPropertyDescription;
        set => SetProperty(ref _newPropertyDescription, value);
    }

    private string _newPropertyTitleDeedNumber = string.Empty;
    public string NewPropertyTitleDeedNumber
    {
        get => _newPropertyTitleDeedNumber;
        set => SetProperty(ref _newPropertyTitleDeedNumber, value);
    }

    private string _newPropertyMunicipalValue = "0";
    public string NewPropertyMunicipalValue
    {
        get => _newPropertyMunicipalValue;
        set => SetProperty(ref _newPropertyMunicipalValue, value);
    }

    // ─────────────────────────────────────────────
    // Quick-add: Financial Claim
    // ─────────────────────────────────────────────

    private string _newClaimInstitutionName = string.Empty;
    public string NewClaimInstitutionName
    {
        get => _newClaimInstitutionName;
        set => SetProperty(ref _newClaimInstitutionName, value);
    }

    private string _newClaimInstitutionAddress = string.Empty;
    public string NewClaimInstitutionAddress
    {
        get => _newClaimInstitutionAddress;
        set => SetProperty(ref _newClaimInstitutionAddress, value);
    }

    private string _newClaimAccountReference = string.Empty;
    public string NewClaimAccountReference
    {
        get => _newClaimAccountReference;
        set => SetProperty(ref _newClaimAccountReference, value);
    }

    private string _newClaimValue = "0";
    public string NewClaimValue
    {
        get => _newClaimValue;
        set => SetProperty(ref _newClaimValue, value);
    }

    // ─────────────────────────────────────────────
    // Quick-add: Diary Entry
    // ─────────────────────────────────────────────

    private string _newDiaryNote = string.Empty;
    public string NewDiaryNote
    {
        get => _newDiaryNote;
        set => SetProperty(ref _newDiaryNote, value);
    }

    private DateTime? _newDiaryDueDate = DateTime.Today.AddDays(7);
    public DateTime? NewDiaryDueDate
    {
        get => _newDiaryDueDate;
        set => SetProperty(ref _newDiaryDueDate, value);
    }

    private string _newDiaryNotifyBy = "Email";
    public string NewDiaryNotifyBy
    {
        get => _newDiaryNotifyBy;
        set => SetProperty(ref _newDiaryNotifyBy, value);
    }

    // ─────────────────────────────────────────────
    // Quick-add: Progress Stage
    // ─────────────────────────────────────────────

    private string _newStageName = string.Empty;
    public string NewStageName
    {
        get => _newStageName;
        set => SetProperty(ref _newStageName, value);
    }

    private DateTime? _newStageTargetDate = DateTime.Today;
    public DateTime? NewStageTargetDate
    {
        get => _newStageTargetDate;
        set => SetProperty(ref _newStageTargetDate, value);
    }

    private string _newStageStatus = "InProgress";
    public string NewStageStatus
    {
        get => _newStageStatus;
        set => SetProperty(ref _newStageStatus, value);
    }

    private bool _isEditingProgress;
    public bool IsEditingProgress
    {
        get => _isEditingProgress;
        set => SetProperty(ref _isEditingProgress, value);
    }

    private Guid _editingProgressStageId;
    private string _editingProgressTitle = string.Empty;
    public string EditingProgressTitle
    {
        get => _editingProgressTitle;
        set => SetProperty(ref _editingProgressTitle, value);
    }

    private DateTime? _editingProgressDate = DateTime.Today;
    public DateTime? EditingProgressDate
    {
        get => _editingProgressDate;
        set => SetProperty(ref _editingProgressDate, value);
    }

    private string _editingProgressStatus = "InProgress";
    public string EditingProgressStatus
    {
        get => _editingProgressStatus;
        set => SetProperty(ref _editingProgressStatus, value);
    }

    // ─────────────────────────────────────────────
    // Commands
    // ─────────────────────────────────────────────

    public ICommand BackCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DeleteEstateCommand { get; }

    public ICommand AddExecutorCommand { get; }
    public ICommand DeleteExecutorCommand { get; }

    public ICommand AddBeneficiaryCommand { get; }
    public ICommand DeleteBeneficiaryCommand { get; }

    public ICommand AddMovableAssetCommand { get; }
    public ICommand DeleteMovableAssetCommand { get; }
    public ICommand GenerateVehicleLetterCommand { get; }

    public ICommand AddImmovablePropertyCommand { get; }
    public ICommand DeleteImmovablePropertyCommand { get; }

    public ICommand AddFinancialClaimCommand { get; }
    public ICommand DeleteFinancialClaimCommand { get; }
    public ICommand GenerateInstitutionLetterCommand { get; }

    public ICommand AddDiaryEntryCommand { get; }
    public ICommand OpenDiaryEntryCommand { get; }
    public ICommand ToggleDiaryCompleteCommand { get; }
    public ICommand DeleteDiaryEntryCommand { get; }

    public ICommand AddProgressStageCommand { get; }
    public ICommand EditProgressStageCommand { get; }
    public ICommand SaveProgressStageCommand { get; }
    public ICommand CancelProgressEditCommand { get; }
    public ICommand DeleteProgressStageCommand { get; }

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

        AddExecutorCommand =
            new AsyncRelayCommand(
                async _ => await ExecuteAddExecutorAsync());

        DeleteExecutorCommand =
            new AsyncRelayCommand(
                async parameter =>
                    await ExecuteDeleteExecutorAsync(parameter));

        AddBeneficiaryCommand =
            new AsyncRelayCommand(
                async _ => await ExecuteAddBeneficiaryAsync());

        DeleteBeneficiaryCommand =
            new AsyncRelayCommand(
                async parameter =>
                    await ExecuteDeleteBeneficiaryAsync(parameter));

        AddMovableAssetCommand =
            new AsyncRelayCommand(
                async _ => await ExecuteAddMovableAssetAsync());

        DeleteMovableAssetCommand =
            new AsyncRelayCommand(
                async parameter =>
                    await ExecuteDeleteMovableAssetAsync(parameter));

        GenerateVehicleLetterCommand =
            new AsyncRelayCommand(
                async parameter =>
                    await ExecuteGenerateVehicleLetterAsync(parameter));

        AddImmovablePropertyCommand =
            new AsyncRelayCommand(
                async _ => await ExecuteAddImmovablePropertyAsync());

        DeleteImmovablePropertyCommand =
            new AsyncRelayCommand(
                async parameter =>
                    await ExecuteDeleteImmovablePropertyAsync(parameter));

        AddFinancialClaimCommand =
            new AsyncRelayCommand(
                async _ => await ExecuteAddFinancialClaimAsync());

        DeleteFinancialClaimCommand =
            new AsyncRelayCommand(
                async parameter =>
                    await ExecuteDeleteFinancialClaimAsync(parameter));

        GenerateInstitutionLetterCommand =
            new AsyncRelayCommand(
                async parameter =>
                    await ExecuteGenerateInstitutionLetterAsync(parameter));

        AddDiaryEntryCommand =
            new AsyncRelayCommand(
                async _ => await ExecuteAddDiaryEntryAsync());

        OpenDiaryEntryCommand =
            new RelayCommand(
                parameter => ExecuteOpenDiaryEntry(parameter));

        ToggleDiaryCompleteCommand =
            new AsyncRelayCommand(
                async parameter =>
                    await ExecuteToggleDiaryCompleteAsync(parameter));

        DeleteDiaryEntryCommand =
            new AsyncRelayCommand(
                async parameter =>
                    await ExecuteDeleteDiaryEntryAsync(parameter));

        AddProgressStageCommand =
            new AsyncRelayCommand(
                async _ => await ExecuteAddProgressStageAsync());

        EditProgressStageCommand =
            new RelayCommand(
                parameter => ExecuteBeginEditProgressStage(parameter));

        SaveProgressStageCommand =
            new AsyncRelayCommand(
                async _ => await ExecuteSaveProgressStageAsync());

        CancelProgressEditCommand =
            new RelayCommand(
                _ => CancelProgressEdit());

        DeleteProgressStageCommand =
            new AsyncRelayCommand(
                async parameter =>
                    await ExecuteDeleteProgressStageAsync(parameter));
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
    }

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

    // ─────────────────────────────────────────────
    // Executor
    // ─────────────────────────────────────────────

    private async Task ExecuteAddExecutorAsync()
    {
        if (_estateId == Guid.Empty)
        {
            StatusMessage =
                "No estate is currently open.";
            return;
        }

        if (string.IsNullOrWhiteSpace(
                NewExecutorFullName))
        {
            StatusMessage =
                "Executor full name is required.";
            return;
        }

        if (string.IsNullOrWhiteSpace(
                NewExecutorEmail))
        {
            StatusMessage =
                "Executor email is required.";
            return;
        }

        try
        {
            IsLoading = true;

            StatusMessage =
                "Saving executor...";

            var request =
                new CreateExecutorRequest
                {
                    FullName =
                        NewExecutorFullName.Trim(),

                    IdNumber =
                        NewExecutorIdNumber?.Trim()
                        ?? string.Empty,

                    Email =
                        NewExecutorEmail.Trim(),

                    Role =
                        string.IsNullOrWhiteSpace(
                            NewExecutorRole)
                            ? "Nominated Executor"
                            : NewExecutorRole.Trim()
                };

            await _apiClient.AddExecutorAsync(
                _estateId,
                request);

            // Reload the estate from the API/SQL.
            // This proves we are displaying persisted data,
            // rather than merely adding something locally.
            var refreshedEstate =
                await _apiClient.GetEstateAsync(
                    _estateId);

            if (refreshedEstate is null)
            {
                StatusMessage =
                    "Executor was saved, but the estate could not be refreshed.";

                return;
            }

            Estate = refreshedEstate;

            NewExecutorFullName =
                string.Empty;

            NewExecutorIdNumber =
                string.Empty;

            NewExecutorEmail =
                string.Empty;

            NewExecutorRole =
                "Nominated Executor";

            StatusMessage =
                "Executor added successfully.";
        }
        catch (Exception ex)
        {
            StatusMessage =
                $"Could not add executor: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ExecuteDeleteExecutorAsync(
        object? parameter)
    {
        if (parameter is not Guid executorId)
        {
            StatusMessage =
                "Could not determine which executor to remove.";

            return;
        }

        if (_estateId == Guid.Empty)
        {
            StatusMessage =
                "No estate is currently open.";

            return;
        }

        try
        {
            IsLoading = true;

            StatusMessage =
                "Removing executor...";

            await _apiClient.DeleteExecutorAsync(
                _estateId,
                executorId);

            var refreshedEstate =
                await _apiClient.GetEstateAsync(
                    _estateId);

            if (refreshedEstate is null)
            {
                StatusMessage =
                    "Executor was removed, but the estate could not be refreshed.";

                return;
            }

            Estate = refreshedEstate;

            StatusMessage =
                "Executor removed successfully.";
        }
        catch (Exception ex)
        {
            StatusMessage =
                $"Could not remove executor: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
    // ─────────────────────────────────────────────
    // Beneficiary
    // ─────────────────────────────────────────────

    private async Task ExecuteAddBeneficiaryAsync()
    {
        if (string.IsNullOrWhiteSpace(
                NewBeneficiaryFullName))
        {
            StatusMessage =
                "Beneficiary full name is required.";
            return;
        }

        if (string.IsNullOrWhiteSpace(
                NewBeneficiaryEmail))
        {
            StatusMessage =
                "Beneficiary email is required.";
            return;
        }

        try
        {
            await _apiClient.AddBeneficiaryAsync(
                _estateId,
                new CreateBeneficiaryRequest
                {
                    FullName = NewBeneficiaryFullName,
                    IdNumber = NewBeneficiaryIdNumber,
                    Email = NewBeneficiaryEmail.Trim(),
                    Relationship =
                        NewBeneficiaryRelationship,
                    HeirType =
                        NewBeneficiaryHeirType
                });

            NewBeneficiaryFullName = string.Empty;
            NewBeneficiaryIdNumber = string.Empty;
            NewBeneficiaryEmail = string.Empty;
            NewBeneficiaryRelationship = string.Empty;
            NewBeneficiaryHeirType = "Heir";

            await LoadAsync(_estateId);
        }
        catch (Exception ex)
        {
            StatusMessage =
                $"Could not add beneficiary: {ex.Message}";
        }
    }

    private async Task ExecuteDeleteBeneficiaryAsync(
        object? parameter)
    {
        if (parameter is not Guid beneficiaryId)
            return;

        try
        {
            await _apiClient.DeleteBeneficiaryAsync(
                _estateId,
                beneficiaryId);

            await LoadAsync(_estateId);
        }
        catch (Exception ex)
        {
            StatusMessage =
                $"Could not remove beneficiary: {ex.Message}";
        }
    }

    // ─────────────────────────────────────────────
    // Movable Asset
    // ─────────────────────────────────────────────

    private async Task ExecuteAddMovableAssetAsync()
    {
        if (string.IsNullOrWhiteSpace(
                NewAssetDescription))
        {
            StatusMessage =
                "Asset description is required.";
            return;
        }

        decimal.TryParse(
            NewAssetValue,
            out var value);

        try
        {
            await _apiClient.AddMovableAssetAsync(
                _estateId,
                new CreateMovableAssetRequest
                {
                    Description = NewAssetDescription,
                    AssetType = NewAssetType,
                    Value = value,
                    IsVehicle = NewAssetIsVehicle,
                    RegistrationNumber =
                        NewAssetRegistrationNumber,
                    VinNumber = NewAssetVinNumber,
                    Make = NewAssetMake
                });

            NewAssetDescription = string.Empty;
            NewAssetType = string.Empty;
            NewAssetValue = "0";
            NewAssetIsVehicle = false;
            NewAssetRegistrationNumber = string.Empty;
            NewAssetVinNumber = string.Empty;
            NewAssetMake = string.Empty;

            await LoadAsync(_estateId);
        }
        catch (Exception ex)
        {
            StatusMessage =
                $"Could not add asset: {ex.Message}";
        }
    }

    private async Task ExecuteDeleteMovableAssetAsync(
        object? parameter)
    {
        if (parameter is not Guid assetId)
            return;

        try
        {
            await _apiClient.DeleteMovableAssetAsync(
                _estateId,
                assetId);

            await LoadAsync(_estateId);
        }
        catch (Exception ex)
        {
            StatusMessage =
                $"Could not remove asset: {ex.Message}";
        }
    }

    // ─────────────────────────────────────────────
    // Immovable Property
    // ─────────────────────────────────────────────

    private async Task ExecuteAddImmovablePropertyAsync()
    {
        if (_estateId == Guid.Empty)
        {
            StatusMessage = "No estate is currently open.";
            return;
        }

        if (string.IsNullOrWhiteSpace(NewPropertyDescription))
        {
            StatusMessage = "Property description is required.";
            return;
        }

        decimal.TryParse(
            NewPropertyMunicipalValue,
            out var municipalValue);

        try
        {
            IsLoading = true;
            StatusMessage = "Saving immovable property...";

            await _apiClient.AddImmovablePropertyAsync(
                _estateId,
                new CreateImmovablePropertyRequest
                {
                    Description = NewPropertyDescription.Trim(),
                    TitleDeedNumber =
                        NewPropertyTitleDeedNumber?.Trim()
                        ?? string.Empty,
                    MunicipalValue = municipalValue
                });

            NewPropertyDescription = string.Empty;
            NewPropertyTitleDeedNumber = string.Empty;
            NewPropertyMunicipalValue = "0";

            var refreshedEstate =
                await _apiClient.GetEstateAsync(_estateId);

            if (refreshedEstate is not null)
                Estate = refreshedEstate;

            StatusMessage =
                "Immovable property added successfully.";
        }
        catch (Exception ex)
        {
            StatusMessage =
                $"Could not add immovable property: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ExecuteDeleteImmovablePropertyAsync(
        object? parameter)
    {
        if (parameter is not Guid assetId)
        {
            StatusMessage =
                "Could not determine which property to remove.";
            return;
        }

        try
        {
            IsLoading = true;
            StatusMessage = "Removing immovable property...";

            await _apiClient.DeleteImmovablePropertyAsync(
                _estateId,
                assetId);

            var refreshedEstate =
                await _apiClient.GetEstateAsync(_estateId);

            if (refreshedEstate is not null)
                Estate = refreshedEstate;

            StatusMessage =
                "Immovable property removed successfully.";
        }
        catch (Exception ex)
        {
            StatusMessage =
                $"Could not remove immovable property: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ─────────────────────────────────────────────
    // Letter Generation
    // ─────────────────────────────────────────────

    private async Task ExecuteGenerateVehicleLetterAsync(
        object? parameter)
    {
        if (parameter is not Guid assetId)
        {
            StatusMessage =
                "Could not determine which vehicle to use.";
            return;
        }

        try
        {
            IsLoading = true;
            StatusMessage = "Generating traffic letter...";

            var filePath =
                await _apiClient
                    .GenerateVehicleLetterToTempFileAsync(
                        _estateId,
                        assetId);

            System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true
                });

            StatusMessage =
                "Traffic letter generated successfully.";
        }
        catch (Exception ex)
        {
            StatusMessage =
                $"Could not generate traffic letter: {ex.Message}";

            System.Windows.MessageBox.Show(
                StatusMessage,
                "Letter Generation Failed",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ExecuteGenerateInstitutionLetterAsync(
        object? parameter)
    {
        if (parameter is not Guid claimId)
        {
            StatusMessage =
                "Could not determine which financial claim to use.";
            return;
        }

        try
        {
            IsLoading = true;
            StatusMessage = "Generating institution letter...";

            var filePath =
                await _apiClient
                    .GenerateInstitutionLetterToTempFileAsync(
                        _estateId,
                        claimId);

            System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true
                });

            StatusMessage =
                "Institution letter generated successfully.";
        }
        catch (Exception ex)
        {
            StatusMessage =
                $"Could not generate institution letter: {ex.Message}";

            System.Windows.MessageBox.Show(
                StatusMessage,
                "Letter Generation Failed",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ─────────────────────────────────────────────
    // Financial Claim
    // ─────────────────────────────────────────────

    private async Task ExecuteAddFinancialClaimAsync()
    {
        if (string.IsNullOrWhiteSpace(
                NewClaimInstitutionName))
        {
            StatusMessage =
                "Institution name is required.";
            return;
        }

        decimal.TryParse(
            NewClaimValue,
            out var value);

        try
        {
            await _apiClient.AddFinancialClaimAsync(
                _estateId,
                new CreateFinancialClaimRequest
                {
                    InstitutionName =
                        NewClaimInstitutionName,
                    InstitutionAddress =
                        NewClaimInstitutionAddress,
                    AccountReference =
                        NewClaimAccountReference,
                    Value = value
                });

            NewClaimInstitutionName = string.Empty;
            NewClaimInstitutionAddress = string.Empty;
            NewClaimAccountReference = string.Empty;
            NewClaimValue = "0";

            await LoadAsync(_estateId);
        }
        catch (Exception ex)
        {
            StatusMessage =
                $"Could not add claim: {ex.Message}";
        }
    }

    private async Task ExecuteDeleteFinancialClaimAsync(
        object? parameter)
    {
        if (parameter is not Guid claimId)
            return;

        try
        {
            await _apiClient.DeleteFinancialClaimAsync(
                _estateId,
                claimId);

            await LoadAsync(_estateId);
        }
        catch (Exception ex)
        {
            StatusMessage =
                $"Could not remove claim: {ex.Message}";
        }
    }

    // ─────────────────────────────────────────────
    // Diary
    // ─────────────────────────────────────────────

    private async Task ExecuteAddDiaryEntryAsync()
    {
        if (string.IsNullOrWhiteSpace(NewDiaryNote))
        {
            StatusMessage =
                "Diary note is required.";
            return;
        }

        if (!NewDiaryDueDate.HasValue)
        {
            StatusMessage =
                "Diary due date is required.";
            return;
        }

        try
        {
            StatusMessage =
                "Adding diary entry...";

            await _apiClient.AddDiaryEntryAsync(
                _estateId,
                new CreateDiaryEntryRequest
                {
                    Note = NewDiaryNote.Trim(),
                    DueDate = NewDiaryDueDate.Value,
                    NotifyBy = NewDiaryNotifyBy
                });

            NewDiaryNote = string.Empty;
            NewDiaryDueDate =
                DateTime.Today.AddDays(7);
            NewDiaryNotifyBy = "Email";

            await LoadAsync(_estateId);

            StatusMessage =
                "Diary entry added.";
        }
        catch (Exception ex)
        {
            StatusMessage =
                $"Could not add diary entry: {ex.Message}";
        }
    }

    private void ExecuteOpenDiaryEntry(
        object? parameter)
    {
        if (parameter is not DiaryEntryDto diaryEntry)
        {
            StatusMessage =
                "Could not open the diary entry.";
            return;
        }

        var status =
            diaryEntry.IsCompleted
                ? "Completed"
                : "Open";

        var message =
            $"{diaryEntry.Note}\n\n" +
            $"Due date: {diaryEntry.DueDate:dd MMMM yyyy}\n" +
            $"Notify by: {diaryEntry.NotifyBy}\n" +
            $"Status: {status}";

        System.Windows.MessageBox.Show(
            message,
            "Diary Entry",
            System.Windows.MessageBoxButton.OK,
            System.Windows.MessageBoxImage.Information);
    }

    private async Task ExecuteToggleDiaryCompleteAsync(
        object? parameter)
    {
        if (parameter is not DiaryEntryDto diaryEntry)
            return;

        try
        {
            var newStatus =
                !diaryEntry.IsCompleted;

            await _apiClient.UpdateDiaryEntryStatusAsync(
                _estateId,
                diaryEntry.DiaryId,
                newStatus);

            await LoadAsync(_estateId);

            StatusMessage =
                newStatus
                    ? "Diary entry marked complete."
                    : "Diary entry reopened.";
        }
        catch (Exception ex)
        {
            StatusMessage =
                $"Could not update diary entry: {ex.Message}";
        }
    }

    private async Task ExecuteDeleteDiaryEntryAsync(
        object? parameter)
    {
        if (parameter is not Guid diaryId)
            return;

        try
        {
            await _apiClient.DeleteDiaryEntryAsync(
                _estateId,
                diaryId);

            await LoadAsync(_estateId);

            StatusMessage =
                "Diary entry removed.";
        }
        catch (Exception ex)
        {
            StatusMessage =
                $"Could not remove diary entry: {ex.Message}";
        }
    }

    // ─────────────────────────────────────────────
    // Progress
    // ─────────────────────────────────────────────

    private async Task ExecuteAddProgressStageAsync()
    {
        if (_estateId == Guid.Empty)
        {
            StatusMessage = "No estate is currently open.";
            return;
        }

        if (string.IsNullOrWhiteSpace(NewStageName))
        {
            StatusMessage = "Progress update is required.";
            return;
        }

        try
        {
            IsLoading = true;
            StatusMessage = "Adding progress update...";

            await _apiClient.AddProgressStageAsync(
                _estateId,
                new CreateProgressStageRequest
                {
                    StageName = NewStageName.Trim(),
                    TargetDate = NewStageTargetDate ?? DateTime.Today,
                    Status = NewStageStatus
                });

            NewStageName = string.Empty;
            NewStageTargetDate = DateTime.Today;
            NewStageStatus = "InProgress";

            await RefreshEstateAsync();

            StatusMessage =
                "Progress update added.";
        }
        catch (Exception ex)
        {
            StatusMessage =
                $"Could not add progress update: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ExecuteBeginEditProgressStage(
        object? parameter)
    {
        if (parameter is not ProgressStageDto stage)
        {
            StatusMessage =
                "Could not determine which progress update to edit.";
            return;
        }

        _editingProgressStageId = stage.StageId;
        EditingProgressTitle = stage.StageName;
        EditingProgressDate = stage.TargetDate ?? DateTime.Today;
        EditingProgressStatus =
            string.IsNullOrWhiteSpace(stage.Status)
                ? "InProgress"
                : stage.Status;

        IsEditingProgress = true;
        StatusMessage = string.Empty;
    }

    private async Task ExecuteSaveProgressStageAsync()
    {
        if (_editingProgressStageId == Guid.Empty)
        {
            StatusMessage =
                "Choose a progress update to edit first.";
            return;
        }

        if (string.IsNullOrWhiteSpace(EditingProgressTitle))
        {
            StatusMessage =
                "Progress update is required.";
            return;
        }

        try
        {
            IsLoading = true;
            StatusMessage = "Saving progress update...";

            await _apiClient.UpdateProgressStageAsync(
                _estateId,
                new UpdateProgressStageRequest
                {
                    StageId = _editingProgressStageId,
                    StageName = EditingProgressTitle.Trim(),
                    TargetDate = EditingProgressDate ?? DateTime.Today,
                    Status = EditingProgressStatus
                });

            CancelProgressEdit();
            await RefreshEstateAsync();

            StatusMessage =
                "Progress update saved.";
        }
        catch (Exception ex)
        {
            StatusMessage =
                $"Could not save progress update: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void CancelProgressEdit()
    {
        _editingProgressStageId = Guid.Empty;
        EditingProgressTitle = string.Empty;
        EditingProgressDate = DateTime.Today;
        EditingProgressStatus = "InProgress";
        IsEditingProgress = false;
    }

    private async Task ExecuteDeleteProgressStageAsync(
        object? parameter)
    {
        if (parameter is not Guid stageId)
        {
            StatusMessage =
                "Could not determine which progress update to remove.";
            return;
        }

        try
        {
            IsLoading = true;
            StatusMessage = "Removing progress update...";

            await _apiClient.DeleteProgressStageAsync(
                _estateId,
                stageId);

            if (_editingProgressStageId == stageId)
                CancelProgressEdit();

            await RefreshEstateAsync();

            StatusMessage =
                "Progress update removed.";
        }
        catch (Exception ex)
        {
            StatusMessage =
                $"Could not remove progress update: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task RefreshEstateAsync()
    {
        var refreshedEstate =
            await _apiClient.GetEstateAsync(_estateId);

        if (refreshedEstate is not null)
            Estate = refreshedEstate;
    }

}