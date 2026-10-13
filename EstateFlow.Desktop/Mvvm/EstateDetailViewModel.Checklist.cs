using System.Collections.ObjectModel;
using System.Windows.Input;
using EstateFlow.Core.Dtos;

namespace EstateFlow.Desktop.Mvvm;

// The Master's-process stage checklist: report the estate → Letters of
// Executorship → advertise for creditors → lodge the account → inspection →
// pay out → finalised. Deadlines are worked out by the API.
public partial class EstateDetailViewModel
{
    public ObservableCollection<MilestoneRowViewModel> Milestones { get; } = new();

    public ICommand CompleteMilestoneCommand { get; private set; } = null!;
    public ICommand ReopenMilestoneCommand { get; private set; } = null!;

    public string? EarliestAccountLodgingText =>
        Estate?.Progress?.EarliestAccountLodgingDate is DateTime date
            ? $"Creditor claim period ends {date:d MMMM yyyy}. Do not lodge the account before then."
            : null;

    private void InitializeChecklistCommands()
    {
        CompleteMilestoneCommand =
            new AsyncRelayCommand(async parameter => await ExecuteCompleteMilestoneAsync(parameter));

        ReopenMilestoneCommand =
            new AsyncRelayCommand(async parameter => await ExecuteReopenMilestoneAsync(parameter));
    }

    private void RebuildChecklist()
    {
        Milestones.Clear();

        foreach (var milestone in Estate?.Progress?.Milestones ?? new List<MilestoneDto>())
            Milestones.Add(new MilestoneRowViewModel(milestone));

        OnPropertyChanged(nameof(EarliestAccountLodgingText));
    }

    private async Task ExecuteCompleteMilestoneAsync(object? parameter)
    {
        if (parameter is not MilestoneRowViewModel row)
            return;

        if (row.CompletedOnInput is not DateTime completedOn)
        {
            StatusMessage = $"Choose the date \"{row.Title}\" was completed.";
            return;
        }

        var request =
            new CompleteMilestoneRequest
            {
                CompletedOn = completedOn,
                Notes = row.NotesInput
            };

        await RunAsync(
            "Updating checklist...",
            () => _apiClient.CompleteMilestoneAsync(_estateId, row.Stage, request),
            $"\"{row.Title}\" recorded as completed on {completedOn:d MMMM yyyy}.",
            "Could not update the checklist");
    }

    private async Task ExecuteReopenMilestoneAsync(object? parameter)
    {
        if (parameter is not MilestoneRowViewModel row)
            return;

        if (!Confirm($"Mark \"{row.Title}\" as not completed?", "Reopen Stage"))
            return;

        await RunAsync(
            "Updating checklist...",
            () => _apiClient.ReopenMilestoneAsync(_estateId, row.Stage),
            $"\"{row.Title}\" reopened.",
            "Could not update the checklist");
    }
}

// One row of the checklist, with inputs for recording completion.
public class MilestoneRowViewModel : BaseViewModel
{
    public MilestoneRowViewModel(MilestoneDto milestone)
    {
        Stage = milestone.Stage;
        Order = milestone.Order;
        Title = milestone.Title;
        Description = milestone.Description;
        CompletedOn = milestone.CompletedOn;
        Notes = milestone.Notes;
        DueDate = milestone.DueDate;
        DeadlineBasis = milestone.DeadlineBasis;
        IsStatutoryDeadline = milestone.IsStatutoryDeadline;
        IsOverdue = milestone.IsOverdue;
        IsDueSoon = milestone.IsDueSoon;
        IsNext = milestone.IsNext;

        _completedOnInput = milestone.CompletedOn ?? DateTime.Today;
        _notesInput = milestone.Notes ?? string.Empty;
    }

    public string Stage { get; }
    public int Order { get; }
    public string Title { get; }
    public string Description { get; }
    public DateTime? CompletedOn { get; }
    public string? Notes { get; }
    public DateTime? DueDate { get; }
    public string? DeadlineBasis { get; }
    public bool IsStatutoryDeadline { get; }
    public bool IsOverdue { get; }
    public bool IsDueSoon { get; }
    public bool IsNext { get; }

    public bool IsComplete => CompletedOn.HasValue;
    public bool IsOpen => !IsComplete;

    public string Marker =>
        IsComplete ? "✓" : IsNext ? "→" : "○";

    // One line describing where this stage stands.
    public string StatusText
    {
        get
        {
            if (CompletedOn is DateTime done)
                return $"Completed {done:d MMM yyyy}";

            if (DueDate is not DateTime due)
                return IsNext ? "Next — no deadline until the previous stage is done" : "Not started";

            var kind = IsStatutoryDeadline ? "Statutory deadline" : "Follow up by";

            if (IsOverdue)
            {
                var days = (int)(DateTime.Today - due.Date).TotalDays;
                return $"OVERDUE by {days} day{(days == 1 ? "" : "s")} — {kind.ToLowerInvariant()} was {due:d MMM yyyy}";
            }

            return $"{kind} {due:d MMM yyyy}";
        }
    }

    // "Overdue" | "DueSoon" | "Complete" | "Next" | "Pending" — for colouring.
    public string State =>
        IsComplete ? "Complete" : IsOverdue ? "Overdue" : IsDueSoon ? "DueSoon" : IsNext ? "Next" : "Pending";

    private DateTime? _completedOnInput;
    public DateTime? CompletedOnInput
    {
        get => _completedOnInput;
        set => SetProperty(ref _completedOnInput, value);
    }

    private string _notesInput;
    public string NotesInput
    {
        get => _notesInput;
        set => SetProperty(ref _notesInput, value);
    }
}
