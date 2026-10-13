using System.Windows.Input;
using EstateFlow.Core.Dtos;

namespace EstateFlow.Desktop.Mvvm;

// Diary entries (notes with due dates) for this estate.
public partial class EstateDetailViewModel
{
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

    // Email: morning reminder email. InApp: Dashboard "Due" panel only.
    // Both: email and Dashboard. (Every open entry also shows on the Dashboard.)
    private string _newDiaryNotifyBy = "Both";
    public string NewDiaryNotifyBy
    {
        get => _newDiaryNotifyBy;
        set => SetProperty(ref _newDiaryNotifyBy, value);
    }

    public ICommand AddDiaryEntryCommand { get; private set; } = null!;
    public ICommand OpenDiaryEntryCommand { get; private set; } = null!;
    public ICommand ToggleDiaryCompleteCommand { get; private set; } = null!;
    public ICommand DeleteDiaryEntryCommand { get; private set; } = null!;

    private void InitializeDiaryCommands()
    {
        AddDiaryEntryCommand =
            new AsyncRelayCommand(async _ => await ExecuteAddDiaryEntryAsync());

        OpenDiaryEntryCommand =
            new RelayCommand(ExecuteOpenDiaryEntry);

        ToggleDiaryCompleteCommand =
            new AsyncRelayCommand(async parameter => await ExecuteToggleDiaryCompleteAsync(parameter));

        DeleteDiaryEntryCommand =
            new AsyncRelayCommand(async parameter => await ExecuteDeleteDiaryEntryAsync(parameter));
    }

    private async Task ExecuteAddDiaryEntryAsync()
    {
        if (string.IsNullOrWhiteSpace(NewDiaryNote))
        {
            StatusMessage = "Diary note is required.";
            return;
        }

        if (!NewDiaryDueDate.HasValue)
        {
            StatusMessage = "Diary due date is required.";
            return;
        }

        var request =
            new CreateDiaryEntryRequest
            {
                Note = NewDiaryNote.Trim(),
                DueDate = NewDiaryDueDate.Value,
                NotifyBy = NewDiaryNotifyBy
            };

        await RunAsync(
            "Adding diary entry...",
            async () =>
            {
                await _apiClient.AddDiaryEntryAsync(_estateId, request);

                NewDiaryNote = string.Empty;
                NewDiaryDueDate = DateTime.Today.AddDays(7);
                NewDiaryNotifyBy = "Both";
            },
            "Diary entry added.",
            "Could not add diary entry");
    }

    private void ExecuteOpenDiaryEntry(object? parameter)
    {
        if (parameter is not DiaryEntryDto diaryEntry)
        {
            StatusMessage = "Could not open the diary entry.";
            return;
        }

        var status =
            diaryEntry.IsCompleted
                ? "Completed"
                : diaryEntry.DueDate.Date < DateTime.Today
                    ? "Overdue"
                    : "Open";

        var message =
            $"{diaryEntry.Note}\n\n" +
            $"Due date: {diaryEntry.DueDate:dd MMMM yyyy}\n" +
            $"Reminder: {diaryEntry.NotifyBy}\n" +
            $"Status: {status}";

        System.Windows.MessageBox.Show(
            message,
            "Diary Entry",
            System.Windows.MessageBoxButton.OK,
            System.Windows.MessageBoxImage.Information);
    }

    private async Task ExecuteToggleDiaryCompleteAsync(object? parameter)
    {
        if (parameter is not DiaryEntryDto diaryEntry)
            return;

        var newStatus = !diaryEntry.IsCompleted;

        await RunAsync(
            "Updating diary entry...",
            () => _apiClient.UpdateDiaryEntryStatusAsync(_estateId, diaryEntry.DiaryId, newStatus),
            newStatus ? "Diary entry marked complete." : "Diary entry reopened.",
            "Could not update diary entry");
    }

    private async Task ExecuteDeleteDiaryEntryAsync(object? parameter)
    {
        if (parameter is not Guid diaryId)
            return;

        if (!Confirm("Remove this diary entry?", "Remove Diary Entry"))
            return;

        await RunAsync(
            "Removing diary entry...",
            () => _apiClient.DeleteDiaryEntryAsync(_estateId, diaryId),
            "Diary entry removed.",
            "Could not remove diary entry");
    }
}
