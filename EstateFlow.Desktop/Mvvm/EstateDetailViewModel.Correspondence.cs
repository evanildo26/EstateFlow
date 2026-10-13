using System.Collections.ObjectModel;
using System.Windows.Input;
using EstateFlow.Core.Dtos;

namespace EstateFlow.Desktop.Mvvm;

// Every email and letter sent from EstateFlow for this estate.
public partial class EstateDetailViewModel
{
    public ObservableCollection<CorrespondenceDto> Correspondence { get; } = new();

    private string _correspondenceMessage = string.Empty;
    public string CorrespondenceMessage
    {
        get => _correspondenceMessage;
        set => SetProperty(ref _correspondenceMessage, value);
    }

    public ICommand RefreshCorrespondenceCommand { get; private set; } = null!;
    public ICommand OpenCorrespondenceDocumentCommand { get; private set; } = null!;

    private void InitializeCorrespondenceCommands()
    {
        RefreshCorrespondenceCommand =
            new AsyncRelayCommand(async _ => await LoadCorrespondenceAsync());

        OpenCorrespondenceDocumentCommand =
            new AsyncRelayCommand(async parameter => await ExecuteOpenCorrespondenceDocumentAsync(parameter));
    }

    private async Task LoadCorrespondenceAsync()
    {
        if (_estateId == Guid.Empty)
            return;

        try
        {
            var history = await _apiClient.GetCorrespondenceAsync(_estateId);

            Correspondence.Clear();
            foreach (var item in history)
                Correspondence.Add(item);

            CorrespondenceMessage =
                Correspondence.Count == 0
                    ? "Nothing has been sent from EstateFlow for this estate yet."
                    : string.Empty;
        }
        catch (Exception ex)
        {
            CorrespondenceMessage = $"The correspondence history could not be loaded: {ex.Message}";
        }
    }

    // Letters are filed as documents; open the copy that was sent.
    private async Task ExecuteOpenCorrespondenceDocumentAsync(object? parameter)
    {
        if (parameter is not CorrespondenceDto { DocumentId: Guid documentId } item || Estate is null)
            return;

        var document = Estate.ScannedDocuments.FirstOrDefault(d => d.DocumentId == documentId);

        try
        {
            IsLoading = true;
            StatusMessage = "Opening letter...";

            var path = await _apiClient.DownloadDocumentToTempFileAsync(
                _estateId,
                documentId,
                document?.FileName ?? $"{item.Kind}.docx");

            OpenFile(path);
            StatusMessage = string.Empty;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Could not open the letter: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
