using System.Windows.Input;
using EstateFlow.Core.Dtos;
using EstateFlow.Core.Validation;

namespace EstateFlow.Desktop.Mvvm;

// Executors and heirs/beneficiaries, and inviting them to the progress portal.
public partial class EstateDetailViewModel
{
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
        set
        {
            if (SetProperty(ref _newExecutorIdNumber, value))
                OnPropertyChanged(nameof(NewExecutorIdHint));
        }
    }

    // Shown under the ID box as the controller types, so a typo is caught
    // before it reaches a letter.
    public string NewExecutorIdHint =>
        IdHint(NewExecutorIdNumber);

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
        set
        {
            if (SetProperty(ref _newBeneficiaryIdNumber, value))
                OnPropertyChanged(nameof(NewBeneficiaryIdHint));
        }
    }

    public string NewBeneficiaryIdHint =>
        IdHint(NewBeneficiaryIdNumber);

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
    // Commands
    // ─────────────────────────────────────────────

    public ICommand AddExecutorCommand { get; private set; } = null!;
    public ICommand DeleteExecutorCommand { get; private set; } = null!;
    public ICommand InviteExecutorToPortalCommand { get; private set; } = null!;

    public ICommand AddBeneficiaryCommand { get; private set; } = null!;
    public ICommand DeleteBeneficiaryCommand { get; private set; } = null!;
    public ICommand InviteBeneficiaryToPortalCommand { get; private set; } = null!;

    private void InitializePeopleCommands()
    {
        AddExecutorCommand =
            new AsyncRelayCommand(
                async _ => await ExecuteAddExecutorAsync());

        DeleteExecutorCommand =
            new AsyncRelayCommand(
                async parameter =>
                    await ExecuteDeleteExecutorAsync(parameter));

        InviteExecutorToPortalCommand =
            new AsyncRelayCommand(
                async parameter =>
                    await ExecuteInviteAsync("Executor", parameter));

        AddBeneficiaryCommand =
            new AsyncRelayCommand(
                async _ => await ExecuteAddBeneficiaryAsync());

        DeleteBeneficiaryCommand =
            new AsyncRelayCommand(
                async parameter =>
                    await ExecuteDeleteBeneficiaryAsync(parameter));

        InviteBeneficiaryToPortalCommand =
            new AsyncRelayCommand(
                async parameter =>
                    await ExecuteInviteAsync("Beneficiary", parameter));
    }

    private static string IdHint(string? idNumber) =>
        SouthAfricanIdNumber.Check(idNumber).Error ?? string.Empty;

    // ─────────────────────────────────────────────
    // Executor
    // ─────────────────────────────────────────────

    private async Task ExecuteAddExecutorAsync()
    {
        if (string.IsNullOrWhiteSpace(NewExecutorFullName))
        {
            StatusMessage = "Executor full name is required.";
            return;
        }

        if (string.IsNullOrWhiteSpace(NewExecutorEmail))
        {
            StatusMessage = "Executor email is required.";
            return;
        }

        if (NewExecutorIdHint.Length > 0)
        {
            StatusMessage = $"Executor ID number: {NewExecutorIdHint}";
            return;
        }

        var request =
            new CreateExecutorRequest
            {
                FullName = NewExecutorFullName.Trim(),
                IdNumber = NewExecutorIdNumber?.Trim() ?? string.Empty,
                Email = NewExecutorEmail.Trim(),
                Role =
                    string.IsNullOrWhiteSpace(NewExecutorRole)
                        ? "Nominated Executor"
                        : NewExecutorRole.Trim()
            };

        await RunAsync(
            "Saving executor...",
            async () =>
            {
                await _apiClient.AddExecutorAsync(_estateId, request);

                NewExecutorFullName = string.Empty;
                NewExecutorIdNumber = string.Empty;
                NewExecutorEmail = string.Empty;
                NewExecutorRole = "Nominated Executor";
            },
            "Executor added successfully.",
            "Could not add executor");
    }

    private async Task ExecuteDeleteExecutorAsync(object? parameter)
    {
        if (parameter is not Guid executorId)
        {
            StatusMessage = "Could not determine which executor to remove.";
            return;
        }

        await RunAsync(
            "Removing executor...",
            () => _apiClient.DeleteExecutorAsync(_estateId, executorId),
            "Executor removed successfully.",
            "Could not remove executor");
    }

    // ─────────────────────────────────────────────
    // Beneficiary
    // ─────────────────────────────────────────────

    private async Task ExecuteAddBeneficiaryAsync()
    {
        if (string.IsNullOrWhiteSpace(NewBeneficiaryFullName))
        {
            StatusMessage = "Beneficiary full name is required.";
            return;
        }

        if (string.IsNullOrWhiteSpace(NewBeneficiaryEmail))
        {
            StatusMessage = "Beneficiary email is required.";
            return;
        }

        if (NewBeneficiaryIdHint.Length > 0)
        {
            StatusMessage = $"Beneficiary ID number: {NewBeneficiaryIdHint}";
            return;
        }

        var request =
            new CreateBeneficiaryRequest
            {
                FullName = NewBeneficiaryFullName.Trim(),
                IdNumber = NewBeneficiaryIdNumber?.Trim() ?? string.Empty,
                Email = NewBeneficiaryEmail.Trim(),
                Relationship = NewBeneficiaryRelationship,
                HeirType = NewBeneficiaryHeirType
            };

        await RunAsync(
            "Saving beneficiary...",
            async () =>
            {
                await _apiClient.AddBeneficiaryAsync(_estateId, request);

                NewBeneficiaryFullName = string.Empty;
                NewBeneficiaryIdNumber = string.Empty;
                NewBeneficiaryEmail = string.Empty;
                NewBeneficiaryRelationship = string.Empty;
                NewBeneficiaryHeirType = "Heir";
            },
            "Beneficiary added successfully.",
            "Could not add beneficiary");
    }

    private async Task ExecuteDeleteBeneficiaryAsync(object? parameter)
    {
        if (parameter is not Guid beneficiaryId)
            return;

        await RunAsync(
            "Removing beneficiary...",
            () => _apiClient.DeleteBeneficiaryAsync(_estateId, beneficiaryId),
            "Beneficiary removed successfully.",
            "Could not remove beneficiary");
    }

    // ─────────────────────────────────────────────
    // Portal invitation (read-only progress view)
    // ─────────────────────────────────────────────

    private async Task ExecuteInviteAsync(string personType, object? parameter)
    {
        if (parameter is not Guid personId || Estate is null)
            return;

        var person =
            personType == "Executor"
                ? Estate.Executors.FirstOrDefault(x => x.ExecutorId == personId) is { } e ? (e.FullName, e.Email) : default
                : Estate.Beneficiaries.FirstOrDefault(x => x.BeneficiaryId == personId) is { } b ? (b.FullName, b.Email) : default;

        if (string.IsNullOrWhiteSpace(person.Email))
        {
            StatusMessage = "Add an email address for this person before inviting them to the portal.";
            return;
        }

        if (!Confirm(
                $"Email {person.FullName} ({person.Email}) an invitation to the online progress portal?\n\n" +
                "They will be able to see the stages of this estate and recent updates, " +
                "but not ID numbers, values or documents.",
                "Invite to Portal"))
        {
            return;
        }

        try
        {
            IsLoading = true;
            StatusMessage = "Sending invitation...";

            await _apiClient.InviteToPortalAsync(
                _estateId,
                new InvitePortalUserRequest
                {
                    PersonType = personType,
                    PersonId = personId
                });

            StatusMessage = $"Portal invitation emailed to {person.FullName}.";
            await LoadCorrespondenceAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Could not send the invitation: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
