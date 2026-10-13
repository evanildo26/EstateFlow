using System.Windows.Input;
using EstateFlow.Core.Dtos;

namespace EstateFlow.Desktop.Mvvm;

// Debts of the deceased (home loan, credit cards, SARS, funeral costs, ...).
// Needed to draw up the liquidation & distribution account.
public partial class EstateDetailViewModel
{
    private string _newLiabilityCreditorName = string.Empty;
    public string NewLiabilityCreditorName
    {
        get => _newLiabilityCreditorName;
        set => SetProperty(ref _newLiabilityCreditorName, value);
    }

    private string _newLiabilityDescription = string.Empty;
    public string NewLiabilityDescription
    {
        get => _newLiabilityDescription;
        set => SetProperty(ref _newLiabilityDescription, value);
    }

    private string _newLiabilityAmount = "0";
    public string NewLiabilityAmount
    {
        get => _newLiabilityAmount;
        set => SetProperty(ref _newLiabilityAmount, value);
    }

    public ICommand AddLiabilityCommand { get; private set; } = null!;
    public ICommand DeleteLiabilityCommand { get; private set; } = null!;

    private void InitializeLiabilityCommands()
    {
        AddLiabilityCommand =
            new AsyncRelayCommand(async _ => await ExecuteAddLiabilityAsync());

        DeleteLiabilityCommand =
            new AsyncRelayCommand(async parameter => await ExecuteDeleteLiabilityAsync(parameter));
    }

    private async Task ExecuteAddLiabilityAsync()
    {
        if (string.IsNullOrWhiteSpace(NewLiabilityCreditorName))
        {
            StatusMessage = "Creditor name is required.";
            return;
        }

        if (!TryParseRand(NewLiabilityAmount, out var amount))
        {
            StatusMessage = "Enter the amount owed as a number, e.g. 85000.";
            return;
        }

        var request =
            new CreateLiabilityRequest
            {
                CreditorName = NewLiabilityCreditorName.Trim(),
                Description = NewLiabilityDescription,
                Amount = amount
            };

        await RunAsync(
            "Saving liability...",
            async () =>
            {
                await _apiClient.AddLiabilityAsync(_estateId, request);

                NewLiabilityCreditorName = string.Empty;
                NewLiabilityDescription = string.Empty;
                NewLiabilityAmount = "0";
            },
            "Liability added.",
            "Could not add liability");
    }

    private async Task ExecuteDeleteLiabilityAsync(object? parameter)
    {
        if (parameter is not Guid liabilityId)
            return;

        if (!Confirm("Remove this liability from the estate?", "Remove Liability"))
            return;

        await RunAsync(
            "Removing liability...",
            () => _apiClient.DeleteLiabilityAsync(_estateId, liabilityId),
            "Liability removed.",
            "Could not remove liability");
    }
}
