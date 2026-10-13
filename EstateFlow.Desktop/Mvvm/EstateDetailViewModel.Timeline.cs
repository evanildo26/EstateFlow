using System.Windows.Input;
using EstateFlow.Core.Dtos;

namespace EstateFlow.Desktop.Mvvm;

// Free-text progress updates ("Spoke to executor", "Documents received"),
// shown alongside the formal stage checklist.
public partial class EstateDetailViewModel
{
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
            ? "Add the first progress update from the Timeline tab."
            : $"{CurrentProgressDate} • {CurrentProgressStatus}";

    // ─────────────────────────────────────────────
    // Quick-add
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

    // ─────────────────────────────────────────────
    // Edit
    // ─────────────────────────────────────────────

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

    public ICommand AddProgressStageCommand { get; private set; } = null!;
    public ICommand EditProgressStageCommand { get; private set; } = null!;
    public ICommand SaveProgressStageCommand { get; private set; } = null!;
    public ICommand CancelProgressEditCommand { get; private set; } = null!;
    public ICommand DeleteProgressStageCommand { get; private set; } = null!;

    private void InitializeTimelineCommands()
    {
        AddProgressStageCommand =
            new AsyncRelayCommand(async _ => await ExecuteAddProgressStageAsync());

        EditProgressStageCommand =
            new RelayCommand(ExecuteBeginEditProgressStage);

        SaveProgressStageCommand =
            new AsyncRelayCommand(async _ => await ExecuteSaveProgressStageAsync());

        CancelProgressEditCommand =
            new RelayCommand(_ => CancelProgressEdit());

        DeleteProgressStageCommand =
            new AsyncRelayCommand(async parameter => await ExecuteDeleteProgressStageAsync(parameter));
    }

    private async Task ExecuteAddProgressStageAsync()
    {
        if (string.IsNullOrWhiteSpace(NewStageName))
        {
            StatusMessage = "Progress update is required.";
            return;
        }

        var request =
            new CreateProgressStageRequest
            {
                StageName = NewStageName.Trim(),
                TargetDate = NewStageTargetDate ?? DateTime.Today,
                Status = NewStageStatus
            };

        await RunAsync(
            "Adding progress update...",
            async () =>
            {
                await _apiClient.AddProgressStageAsync(_estateId, request);

                NewStageName = string.Empty;
                NewStageTargetDate = DateTime.Today;
                NewStageStatus = "InProgress";
            },
            "Progress update added.",
            "Could not add progress update");
    }

    private void ExecuteBeginEditProgressStage(object? parameter)
    {
        if (parameter is not ProgressStageDto stage)
        {
            StatusMessage = "Could not determine which progress update to edit.";
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
            StatusMessage = "Choose a progress update to edit first.";
            return;
        }

        if (string.IsNullOrWhiteSpace(EditingProgressTitle))
        {
            StatusMessage = "Progress update is required.";
            return;
        }

        var request =
            new UpdateProgressStageRequest
            {
                StageId = _editingProgressStageId,
                StageName = EditingProgressTitle.Trim(),
                TargetDate = EditingProgressDate ?? DateTime.Today,
                Status = EditingProgressStatus
            };

        await RunAsync(
            "Saving progress update...",
            async () =>
            {
                await _apiClient.UpdateProgressStageAsync(_estateId, request);
                CancelProgressEdit();
            },
            "Progress update saved.",
            "Could not save progress update");
    }

    private void CancelProgressEdit()
    {
        _editingProgressStageId = Guid.Empty;
        EditingProgressTitle = string.Empty;
        EditingProgressDate = DateTime.Today;
        EditingProgressStatus = "InProgress";
        IsEditingProgress = false;
    }

    private async Task ExecuteDeleteProgressStageAsync(object? parameter)
    {
        if (parameter is not Guid stageId)
        {
            StatusMessage = "Could not determine which progress update to remove.";
            return;
        }

        await RunAsync(
            "Removing progress update...",
            async () =>
            {
                await _apiClient.DeleteProgressStageAsync(_estateId, stageId);

                if (_editingProgressStageId == stageId)
                    CancelProgressEdit();
            },
            "Progress update removed.",
            "Could not remove progress update");
    }
}
