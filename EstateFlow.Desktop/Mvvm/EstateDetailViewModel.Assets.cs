using System.Globalization;
using System.Windows.Input;
using EstateFlow.Core.Dtos;
using EstateFlow.Desktop.Services;

namespace EstateFlow.Desktop.Mvvm;

// Movable assets (incl. vehicles), immovable property, bank/insurance claims,
// and the letters generated from them.
public partial class EstateDetailViewModel
{
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
    // Commands
    // ─────────────────────────────────────────────

    public ICommand AddMovableAssetCommand { get; private set; } = null!;
    public ICommand DeleteMovableAssetCommand { get; private set; } = null!;
    public ICommand GenerateVehicleLetterCommand { get; private set; } = null!;

    public ICommand AddImmovablePropertyCommand { get; private set; } = null!;
    public ICommand DeleteImmovablePropertyCommand { get; private set; } = null!;

    public ICommand AddFinancialClaimCommand { get; private set; } = null!;
    public ICommand DeleteFinancialClaimCommand { get; private set; } = null!;
    public ICommand GenerateInstitutionLetterCommand { get; private set; } = null!;

    private void InitializeAssetCommands()
    {
        AddMovableAssetCommand =
            new AsyncRelayCommand(async _ => await ExecuteAddMovableAssetAsync());

        DeleteMovableAssetCommand =
            new AsyncRelayCommand(async parameter => await ExecuteDeleteMovableAssetAsync(parameter));

        GenerateVehicleLetterCommand =
            new AsyncRelayCommand(async parameter => await ExecuteGenerateVehicleLetterAsync(parameter));

        AddImmovablePropertyCommand =
            new AsyncRelayCommand(async _ => await ExecuteAddImmovablePropertyAsync());

        DeleteImmovablePropertyCommand =
            new AsyncRelayCommand(async parameter => await ExecuteDeleteImmovablePropertyAsync(parameter));

        AddFinancialClaimCommand =
            new AsyncRelayCommand(async _ => await ExecuteAddFinancialClaimAsync());

        DeleteFinancialClaimCommand =
            new AsyncRelayCommand(async parameter => await ExecuteDeleteFinancialClaimAsync(parameter));

        GenerateInstitutionLetterCommand =
            new AsyncRelayCommand(async parameter => await ExecuteGenerateInstitutionLetterAsync(parameter));
    }

    // Accepts "1 250 000,50", "1,250,000.50" or "R 1250000".
    private static bool TryParseRand(string? text, out decimal amount)
    {
        var cleaned = (text ?? string.Empty)
            .Replace("R", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace(" ", string.Empty)
            .Replace(" ", string.Empty)
            .Trim();

        if (cleaned.Length == 0)
        {
            amount = 0;
            return true;
        }

        // A single comma followed by 1–2 digits is a decimal comma (en-ZA).
        var lastComma = cleaned.LastIndexOf(',');
        if (lastComma >= 0 && !cleaned.Contains('.') && cleaned.Length - lastComma - 1 is 1 or 2)
            cleaned = cleaned.Remove(lastComma, 1).Insert(lastComma, ".");

        cleaned = cleaned.Replace(",", string.Empty);

        return decimal.TryParse(cleaned, NumberStyles.Number, CultureInfo.InvariantCulture, out amount) &&
               amount >= 0;
    }

    // ─────────────────────────────────────────────
    // Movable Asset
    // ─────────────────────────────────────────────

    private async Task ExecuteAddMovableAssetAsync()
    {
        if (string.IsNullOrWhiteSpace(NewAssetDescription))
        {
            StatusMessage = "Asset description is required.";
            return;
        }

        if (!TryParseRand(NewAssetValue, out var value))
        {
            StatusMessage = "Enter the asset value as a number, e.g. 125000.";
            return;
        }

        var request =
            new CreateMovableAssetRequest
            {
                Description = NewAssetDescription.Trim(),
                AssetType = NewAssetType,
                Value = value,
                IsVehicle = NewAssetIsVehicle,
                RegistrationNumber = NewAssetRegistrationNumber,
                VinNumber = NewAssetVinNumber,
                Make = NewAssetMake
            };

        await RunAsync(
            "Saving asset...",
            async () =>
            {
                await _apiClient.AddMovableAssetAsync(_estateId, request);

                NewAssetDescription = string.Empty;
                NewAssetType = string.Empty;
                NewAssetValue = "0";
                NewAssetIsVehicle = false;
                NewAssetRegistrationNumber = string.Empty;
                NewAssetVinNumber = string.Empty;
                NewAssetMake = string.Empty;
            },
            "Asset added successfully.",
            "Could not add asset");
    }

    private async Task ExecuteDeleteMovableAssetAsync(object? parameter)
    {
        if (parameter is not Guid assetId)
            return;

        await RunAsync(
            "Removing asset...",
            () => _apiClient.DeleteMovableAssetAsync(_estateId, assetId),
            "Asset removed successfully.",
            "Could not remove asset");
    }

    // ─────────────────────────────────────────────
    // Immovable Property
    // ─────────────────────────────────────────────

    private async Task ExecuteAddImmovablePropertyAsync()
    {
        if (string.IsNullOrWhiteSpace(NewPropertyDescription))
        {
            StatusMessage = "Property description is required.";
            return;
        }

        if (!TryParseRand(NewPropertyMunicipalValue, out var municipalValue))
        {
            StatusMessage = "Enter the municipal value as a number, e.g. 1450000.";
            return;
        }

        var request =
            new CreateImmovablePropertyRequest
            {
                Description = NewPropertyDescription.Trim(),
                TitleDeedNumber = NewPropertyTitleDeedNumber?.Trim() ?? string.Empty,
                MunicipalValue = municipalValue
            };

        await RunAsync(
            "Saving immovable property...",
            async () =>
            {
                await _apiClient.AddImmovablePropertyAsync(_estateId, request);

                NewPropertyDescription = string.Empty;
                NewPropertyTitleDeedNumber = string.Empty;
                NewPropertyMunicipalValue = "0";
            },
            "Immovable property added successfully.",
            "Could not add immovable property");
    }

    private async Task ExecuteDeleteImmovablePropertyAsync(object? parameter)
    {
        if (parameter is not Guid assetId)
        {
            StatusMessage = "Could not determine which property to remove.";
            return;
        }

        await RunAsync(
            "Removing immovable property...",
            () => _apiClient.DeleteImmovablePropertyAsync(_estateId, assetId),
            "Immovable property removed successfully.",
            "Could not remove immovable property");
    }

    // ─────────────────────────────────────────────
    // Financial Claim
    // ─────────────────────────────────────────────

    private async Task ExecuteAddFinancialClaimAsync()
    {
        if (string.IsNullOrWhiteSpace(NewClaimInstitutionName))
        {
            StatusMessage = "Institution name is required.";
            return;
        }

        if (!TryParseRand(NewClaimValue, out var value))
        {
            StatusMessage = "Enter the claim value as a number, e.g. 25000.";
            return;
        }

        var request =
            new CreateFinancialClaimRequest
            {
                InstitutionName = NewClaimInstitutionName.Trim(),
                InstitutionAddress = NewClaimInstitutionAddress,
                AccountReference = NewClaimAccountReference,
                Value = value
            };

        await RunAsync(
            "Saving claim...",
            async () =>
            {
                await _apiClient.AddFinancialClaimAsync(_estateId, request);

                NewClaimInstitutionName = string.Empty;
                NewClaimInstitutionAddress = string.Empty;
                NewClaimAccountReference = string.Empty;
                NewClaimValue = "0";
            },
            "Claim added successfully.",
            "Could not add claim");
    }

    private async Task ExecuteDeleteFinancialClaimAsync(object? parameter)
    {
        if (parameter is not Guid claimId)
            return;

        await RunAsync(
            "Removing claim...",
            () => _apiClient.DeleteFinancialClaimAsync(_estateId, claimId),
            "Claim removed successfully.",
            "Could not remove claim");
    }

    // ─────────────────────────────────────────────
    // Letter Generation
    // ─────────────────────────────────────────────

    private Task ExecuteGenerateVehicleLetterAsync(object? parameter) =>
        parameter is Guid assetId
            ? GenerateLetterAsync(
                "traffic department letter",
                () => _apiClient.GenerateVehicleLetterToTempFileAsync(_estateId, assetId))
            : Task.CompletedTask;

    private Task ExecuteGenerateInstitutionLetterAsync(object? parameter) =>
        parameter is Guid claimId
            ? GenerateLetterAsync(
                "notification of death letter",
                () => _apiClient.GenerateInstitutionLetterToTempFileAsync(_estateId, claimId))
            : Task.CompletedTask;

    // The letter opens in Word on the firm's letterhead. A copy is filed on
    // the estate's Documents and recorded under Correspondence.
    private async Task GenerateLetterAsync(
        string letterName,
        Func<Task<GeneratedLetterFile>> generate)
    {
        try
        {
            IsLoading = true;
            StatusMessage = $"Generating {letterName}...";

            var letter = await generate();

            OpenFile(letter.Path);

            StatusMessage =
                letter.FiledOnEstate
                    ? $"The {letterName} opened in Word. A copy was filed under Documents and recorded in Correspondence."
                    : $"The {letterName} opened in Word, but a copy could not be filed on the estate. Please upload it under Documents.";

            await RefreshEstateAsync();
            await LoadCorrespondenceAsync();
        }
        catch (Exception ex)
        {
            StatusMessage =
                $"Could not generate the {letterName}: {ex.Message}";

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
}
