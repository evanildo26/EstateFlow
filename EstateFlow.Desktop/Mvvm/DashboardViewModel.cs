using System.Collections.ObjectModel;
using System.Windows.Input;
using EstateFlow.Core.Dtos;
using EstateFlow.Desktop.Services;

namespace EstateFlow.Desktop.Mvvm;

public class DashboardViewModel : BaseViewModel
{
    public const string FilterActive = "Active estates";
    public const string FilterAttention = "Needs attention";
    public const string FilterAll = "All estates";

    public const string SortMostUrgent = "Most urgent first";
    public const string SortFileNumber = "File number";
    public const string SortName = "Deceased name";
    public const string SortLeastProgress = "Least progress";
    public const string SortDateOfDeath = "Date of death (oldest first)";

    private readonly IApiClient _apiClient;
    private readonly INavigationService _navigation;

    private List<EstateSummaryDto> _allEstates = new();

    public ObservableCollection<EstateSummaryDto> Estates { get; } = new();

    // "Overdue / Due this week" — every open diary entry due within 7 days,
    // across all of the controller's estates. This is what the "InApp"
    // (Dashboard) reminder option shows.
    public ObservableCollection<DueItemViewModel> DueItems { get; } = new();

    public IReadOnlyList<string> StatusFilters { get; } =
        new[] { FilterActive, FilterAttention, "Reported", "In Progress", "Finalised", FilterAll };

    public IReadOnlyList<string> SortOptions { get; } =
        new[] { SortMostUrgent, SortFileNumber, SortName, SortLeastProgress, SortDateOfDeath };

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
                ApplyFilter();
        }
    }

    private string _statusFilter = FilterActive;
    public string StatusFilter
    {
        get => _statusFilter;
        set
        {
            if (SetProperty(ref _statusFilter, value))
                ApplyFilter();
        }
    }

    private string _sortOption = SortMostUrgent;
    public string SortOption
    {
        get => _sortOption;
        set
        {
            if (SetProperty(ref _sortOption, value))
                ApplyFilter();
        }
    }

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

    private string _summaryText = string.Empty;
    public string SummaryText
    {
        get => _summaryText;
        set => SetProperty(ref _summaryText, value);
    }

    private string _dueSummary = string.Empty;
    public string DueSummary
    {
        get => _dueSummary;
        set => SetProperty(ref _dueSummary, value);
    }

    public ICommand LoadEstatesCommand { get; }
    public ICommand OpenEstateCommand { get; }
    public ICommand NewEstateCommand { get; }
    public ICommand ClearFiltersCommand { get; }

    public DashboardViewModel(IApiClient apiClient, INavigationService navigation)
    {
        _apiClient = apiClient;
        _navigation = navigation;

        LoadEstatesCommand = new AsyncRelayCommand(async _ => await LoadAsync());
        OpenEstateCommand = new RelayCommand(ExecuteOpenEstate);
        NewEstateCommand = new RelayCommand(_ => _navigation.NavigateToNewEstate());
        ClearFiltersCommand = new RelayCommand(_ =>
        {
            SearchText = string.Empty;
            StatusFilter = FilterAll;
        });

        _ = LoadAsync();
    }

    private void ExecuteOpenEstate(object? parameter)
    {
        if (parameter is Guid estateId)
            _navigation.NavigateToEstateDetail(estateId);
    }

    private async Task LoadAsync()
    {
        IsLoading = true;
        StatusMessage = "Loading estates...";

        var estatesTask = _apiClient.GetEstatesAsync();
        var diaryTask = _apiClient.GetAllDiaryEntriesAsync();

        try
        {
            _allEstates = await estatesTask;
            ApplyFilter();
        }
        catch (Exception ex)
        {
            _allEstates = new();
            Estates.Clear();
            StatusMessage = $"Could not load estates: {ErrorReporter.Describe(ex)}";
        }

        try
        {
            LoadDueItems(await diaryTask);
        }
        catch (Exception ex)
        {
            DueItems.Clear();
            DueSummary = $"Diary could not be loaded: {ErrorReporter.Describe(ex)}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void LoadDueItems(IEnumerable<DiaryEntryWithEstateDto> entries)
    {
        var horizon = DateTime.Today.AddDays(7);

        var due = entries
            .Where(e => !e.IsCompleted && e.DueDate.Date <= horizon)
            .OrderBy(e => e.DueDate)
            .Select(e => new DueItemViewModel(e))
            .ToList();

        DueItems.Clear();
        foreach (var item in due)
            DueItems.Add(item);

        var overdue = due.Count(d => d.IsOverdue);

        DueSummary = due.Count == 0
            ? "Nothing due in the next 7 days."
            : overdue > 0
                ? $"{overdue} overdue · {due.Count - overdue} due this week"
                : $"{due.Count} due this week";
    }

    private void ApplyFilter()
    {
        IEnumerable<EstateSummaryDto> query = _allEstates;

        var search = SearchText.Trim();
        if (search.Length > 0)
        {
            query = query.Where(e =>
                e.FileNumber.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (e.DeceasedFullName?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                e.EstateStatus.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        query = StatusFilter switch
        {
            FilterActive => query.Where(e => e.EstateStatus != "Finalised"),
            FilterAttention => query.Where(e => e.NeedsAttention || e.OverdueDiaryCount > 0),
            FilterAll => query,
            _ => query.Where(e => e.EstateStatus == StatusFilter)
        };

        query = SortOption switch
        {
            SortFileNumber => query.OrderBy(e => e.FileNumber, StringComparer.OrdinalIgnoreCase),
            SortName => query.OrderBy(e => e.DeceasedFullName, StringComparer.OrdinalIgnoreCase),
            SortLeastProgress => query.OrderBy(e => e.ProgressPercent).ThenBy(e => e.DateOfDeath),
            SortDateOfDeath => query.OrderBy(e => e.DateOfDeath),

            // Most urgent: missed deadlines first (longest overdue on top),
            // then overdue diary items, then the nearest upcoming deadline.
            _ => query
                .OrderByDescending(e => e.NeedsAttention)
                .ThenByDescending(e => e.DaysOverdue)
                .ThenByDescending(e => e.OverdueDiaryCount)
                .ThenBy(e => e.NextDeadline ?? DateTime.MaxValue)
                .ThenBy(e => e.FileNumber, StringComparer.OrdinalIgnoreCase)
        };

        var shown = query.ToList();

        Estates.Clear();
        foreach (var estate in shown)
            Estates.Add(estate);

        var attention = _allEstates.Count(e => e.NeedsAttention);
        var active = _allEstates.Count(e => e.EstateStatus != "Finalised");

        SummaryText =
            $"{active} active · {_allEstates.Count} total" +
            (attention > 0 ? $" · {attention} need{(attention == 1 ? "s" : "")} attention" : string.Empty);

        StatusMessage =
            _allEstates.Count == 0
                ? "No estates yet. Use + New Estate to add your first file."
                : shown.Count == 0
                    ? "No estates match your search or filter."
                    : string.Empty;
    }
}

// A diary entry in the Dashboard's "Overdue / Due this week" panel.
public class DueItemViewModel
{
    public DueItemViewModel(DiaryEntryWithEstateDto entry)
    {
        EstateId = entry.EstateId;
        FileNumber = entry.FileNumber;
        DeceasedFullName = entry.DeceasedFullName ?? string.Empty;
        Note = entry.Note;
        DueDate = entry.DueDate;

        var days = (int)(entry.DueDate.Date - DateTime.Today).TotalDays;
        IsOverdue = days < 0;
        WhenText = days switch
        {
            < -1 => $"{-days} days overdue",
            -1 => "1 day overdue",
            0 => "Due today",
            1 => "Due tomorrow",
            _ => $"Due {entry.DueDate:ddd d MMM}"
        };
    }

    public Guid EstateId { get; }
    public string FileNumber { get; }
    public string DeceasedFullName { get; }
    public string Note { get; }
    public DateTime DueDate { get; }
    public bool IsOverdue { get; }
    public string WhenText { get; }
}
