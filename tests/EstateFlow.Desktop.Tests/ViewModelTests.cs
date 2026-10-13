using EstateFlow.Core.Dtos;
using EstateFlow.Desktop.Mvvm;
using EstateFlow.Desktop.Services;

namespace EstateFlow.Desktop.Tests;

[Collection("WPF")]
public class DashboardViewModelTests
{
    private static DashboardViewModel Create() =>
        Wpf.Run(() =>
        {
            var vm = new DashboardViewModel(SampleData.Api().Object, Fake<INavigationService>.Create().Object);
            Wpf.DoEvents();
            return vm;
        });

    [Fact]
    public void Default_view_hides_finalised_estates_and_puts_most_overdue_first()
    {
        var vm = Create();

        Wpf.Run(() =>
        {
            Assert.Equal(new[] { "EST-001", "EST-004", "EST-003" }, vm.Estates.Select(e => e.FileNumber));
            Assert.Equal("3 active · 4 total · 2 need attention", vm.SummaryText);
        });
    }

    [Fact]
    public void Search_matches_name_or_file_number()
    {
        var vm = Create();

        Wpf.Run(() =>
        {
            vm.StatusFilter = DashboardViewModel.FilterAll;

            vm.SearchText = "done";
            Assert.Equal("EST-002", Assert.Single(vm.Estates).FileNumber);

            vm.SearchText = "est-003";
            Assert.Equal("Calm Estate", Assert.Single(vm.Estates).DeceasedFullName);

            vm.SearchText = "nobody";
            Assert.Empty(vm.Estates);
            Assert.Equal("No estates match your search or filter.", vm.StatusMessage);
        });
    }

    [Fact]
    public void Needs_attention_filter_and_sorting()
    {
        var vm = Create();

        Wpf.Run(() =>
        {
            vm.StatusFilter = DashboardViewModel.FilterAttention;
            Assert.Equal(2, vm.Estates.Count);

            vm.StatusFilter = DashboardViewModel.FilterAll;
            vm.SortOption = DashboardViewModel.SortFileNumber;
            Assert.Equal(new[] { "EST-001", "EST-002", "EST-003", "EST-004" }, vm.Estates.Select(e => e.FileNumber));
        });
    }

    // The "InApp" reminder option now has somewhere to show up.
    [Fact]
    public void Due_panel_lists_open_items_due_within_a_week_overdue_first()
    {
        var vm = Create();

        Wpf.Run(() =>
        {
            Assert.Equal(new[] { "Overdue item", "Due soon" }, vm.DueItems.Select(d => d.Note));
            Assert.True(vm.DueItems[0].IsOverdue);
            Assert.Equal("2 days overdue", vm.DueItems[0].WhenText);
            Assert.Equal("1 overdue · 1 due this week", vm.DueSummary);
        });
    }
}

public class ChecklistRowTests
{
    [Fact]
    public void Overdue_statutory_stage_explains_itself()
    {
        var row = new MilestoneRowViewModel(new MilestoneDto
        {
            Stage = "ReportedToMaster",
            Title = "Estate reported to the Master",
            DueDate = DateTime.Today.AddDays(-5),
            IsStatutoryDeadline = true,
            IsOverdue = true,
            IsNext = true
        });

        Assert.Equal("Overdue", row.State);
        Assert.StartsWith("OVERDUE by 5 days", row.StatusText);
        Assert.Contains("statutory deadline", row.StatusText);
        Assert.Equal(DateTime.Today, row.CompletedOnInput);
    }

    [Fact]
    public void Completed_stage_shows_its_date()
    {
        var row = new MilestoneRowViewModel(new MilestoneDto
        {
            Stage = "LettersOfExecutorship",
            Title = "Letters of Executorship issued",
            CompletedOn = new DateTime(2026, 3, 5)
        });

        Assert.Equal("Complete", row.State);
        Assert.Equal("Completed 5 Mar 2026", row.StatusText);
        Assert.Equal("✓", row.Marker);
    }
}

[Collection("WPF")]
public class FormValidationTests
{
    [Fact]
    public void Estate_form_flags_a_mistyped_id_number_immediately()
    {
        Wpf.Run(() =>
        {
            var vm = new EstateFormViewModel(Fake<IApiClient>.Create().Object, Fake<INavigationService>.Create().Object)
            {
                DateOfDeath = new DateTime(2026, 1, 10)
            };

            vm.DeceasedIdNumber = "8001015009088";
            Assert.Contains("check digit", vm.DeceasedIdHint);

            vm.DeceasedIdNumber = "8001015009087";
            Assert.Equal(string.Empty, vm.DeceasedIdHint);

            vm.DateOfDeath = new DateTime(1979, 1, 1);
            Assert.Contains("after the date of death", vm.DeceasedIdHint);
        });
    }

    [Fact]
    public void Executor_with_invalid_id_is_not_sent_to_the_server()
    {
        Wpf.Run(() =>
        {
            var api = SampleData.Api();
            var vm = new EstateDetailViewModel(api.Object, Fake<INavigationService>.Create().Object);
            vm.LoadAsync(SampleData.EstateId).GetAwaiter();
            Wpf.DoEvents();

            vm.NewExecutorFullName = "Exec";
            vm.NewExecutorEmail = "exec@test.local";
            vm.NewExecutorIdNumber = "123";
            vm.AddExecutorCommand.Execute(null);
            Wpf.DoEvents();

            Assert.Contains("ID number", vm.StatusMessage);
            Assert.DoesNotContain(api.Calls, c => c.Method == nameof(IApiClient.AddExecutorAsync));
        });
    }
}
