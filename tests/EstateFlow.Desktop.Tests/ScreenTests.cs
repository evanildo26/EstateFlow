using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using EstateFlow.Core.Dtos;
using EstateFlow.Core.Workflow;
using EstateFlow.Desktop.Mvvm;
using EstateFlow.Desktop.Services;
using EstateFlow.Desktop.Views;
using Microsoft.Extensions.DependencyInjection;

namespace EstateFlow.Desktop.Tests;

public static class SampleData
{
    public static readonly Guid EstateId = Guid.NewGuid();

    public static EstateDto Estate()
    {
        var progress = MasterProcess.Evaluate(
            new DateTime(2026, 1, 10),
            new Dictionary<EstateMilestoneStage, DateTime>
            {
                [EstateMilestoneStage.ReportedToMaster] = new DateTime(2026, 1, 20)
            },
            DateTime.Today);

        return new EstateDto
        {
            EstateId = EstateId,
            FileNumber = "EST-001",
            MastersReference = "1234/2026",
            DateOfDeath = new DateTime(2026, 1, 10),
            EstateStatus = "In Progress",
            Deceased = new DeceasedDto { FullName = "Jan van der Merwe", IdNumber = "8001015009087", MaritalStatus = "Married" },
            Executors = { new ExecutorDto { ExecutorId = Guid.NewGuid(), FullName = "Anna Executor", Email = "anna@test.local", Role = "Nominated Executor" } },
            Beneficiaries = { new BeneficiaryDto { BeneficiaryId = Guid.NewGuid(), FullName = "Thandi Heir", Email = "thandi@test.local", HeirType = "Heir", Relationship = "Daughter" } },
            MovableAssets = { new MovableAssetDto { AssetId = Guid.NewGuid(), Description = "Toyota Corolla", IsVehicle = true, Value = 120000, RegistrationNumber = "CA 123-456" } },
            ImmovableProperties = { new ImmovablePropertyDto { AssetId = Guid.NewGuid(), Description = "Erf 1, Cape Town", MunicipalValue = 1500000 } },
            FinancialClaims = { new FinancialClaimDto { ClaimId = Guid.NewGuid(), InstitutionName = "FNB", AccountReference = "62000000000", Value = 50000 } },
            Liabilities = { new LiabilityDto { LiabilityId = Guid.NewGuid(), CreditorName = "Home loan", Amount = 400000 } },
            DiaryEntries = { new DiaryEntryDto { DiaryId = Guid.NewGuid(), Note = "Chase the bank", DueDate = DateTime.Today.AddDays(-1), NotifyBy = "InApp" } },
            ProgressStages = { new ProgressStageDto { StageId = Guid.NewGuid(), StageName = "Documents received", TargetDate = DateTime.Today, Status = "Complete" } },
            ScannedDocuments = { new ScannedDocumentDto { DocumentId = Guid.NewGuid(), DocumentType = "Death certificate", FileName = "dc.pdf", UploadDate = DateTime.Today } },
            TotalAssets = 1670000,
            TotalLiabilities = 400000,
            Progress = new EstateProgressDto
            {
                PercentComplete = progress.PercentComplete,
                CompletedCount = progress.CompletedCount,
                TotalCount = 7,
                NeedsAttention = true,
                AttentionReason = "Letters of Executorship issued: follow-up date passed.",
                NextMilestoneTitle = progress.NextMilestone?.Title,
                Milestones = progress.Milestones.Select(m => new MilestoneDto
                {
                    Stage = m.Stage.ToString(),
                    Order = (int)m.Stage,
                    Title = m.Title,
                    Description = m.Description,
                    CompletedOn = m.CompletedOn,
                    DueDate = m.DueDate,
                    DeadlineBasis = m.DeadlineBasis,
                    IsStatutoryDeadline = m.IsStatutoryDeadline,
                    IsOverdue = m.IsOverdue,
                    IsDueSoon = m.IsDueSoon,
                    IsNext = m.IsNext
                }).ToList()
            }
        };
    }

    public static List<EstateSummaryDto> Summaries() => new()
    {
        new() { EstateId = Guid.NewGuid(), FileNumber = "EST-003", DeceasedFullName = "Calm Estate", EstateStatus = "In Progress", ProgressPercent = 57, NextDeadline = DateTime.Today.AddDays(40) },
        new() { EstateId = Guid.NewGuid(), FileNumber = "EST-001", DeceasedFullName = "Late Estate", EstateStatus = "Reported", NeedsAttention = true, DaysOverdue = 30, AttentionReason = "Report overdue" },
        new() { EstateId = Guid.NewGuid(), FileNumber = "EST-002", DeceasedFullName = "Done Estate", EstateStatus = "Finalised", ProgressPercent = 100 },
        new() { EstateId = Guid.NewGuid(), FileNumber = "EST-004", DeceasedFullName = "Slightly Late", EstateStatus = "In Progress", NeedsAttention = true, DaysOverdue = 3, OverdueDiaryCount = 2 },
    };

    public static List<DiaryEntryWithEstateDto> Diary() => new()
    {
        new() { EstateId = EstateId, FileNumber = "EST-001", DeceasedFullName = "Late Estate", Note = "Overdue item", DueDate = DateTime.Today.AddDays(-2) },
        new() { EstateId = EstateId, FileNumber = "EST-001", DeceasedFullName = "Late Estate", Note = "Due soon", DueDate = DateTime.Today.AddDays(3) },
        new() { EstateId = EstateId, FileNumber = "EST-001", DeceasedFullName = "Late Estate", Note = "Far away", DueDate = DateTime.Today.AddDays(30) },
        new() { EstateId = EstateId, FileNumber = "EST-001", DeceasedFullName = "Late Estate", Note = "Done", DueDate = DateTime.Today, IsCompleted = true },
    };

    public static Fake<IApiClient> Api() =>
        Fake<IApiClient>.Create()
            .Returns(nameof(IApiClient.GetEstatesAsync), Summaries())
            .Returns(nameof(IApiClient.GetAllDiaryEntriesAsync), Diary())
            .Returns(nameof(IApiClient.GetEstateAsync), Estate())
            .Returns(nameof(IApiClient.GetStaffAsync), new List<StaffMemberDto>
            {
                new() { UserId = "1", ControllerId = Guid.NewGuid(), FullName = "S. Abrahams", Email = "s@test.local", Role = "EstateController", AssignedEstateCount = 3 },
                new() { UserId = "2", ControllerId = Guid.NewGuid(), FullName = "Gone Person", Email = "g@test.local", Role = "EstateController", IsDeactivated = true }
            })
            .Returns(nameof(IApiClient.GetAllEstatesAsync), new List<AdminEstateDto>
            {
                new() { EstateId = Guid.NewGuid(), FileNumber = "EST-001", DeceasedFullName = "Late Estate", ControllerName = "S. Abrahams" }
            })
            .Returns(nameof(IApiClient.GetLetterTemplatesAsync), new List<LetterTemplateDto>
            {
                new() { LetterType = "VehicleTransfer", DisplayName = "Vehicle transfer", Subject = "S", Body = "B", Placeholders = { "{FileNumber}" } }
            })
            .Returns(nameof(IApiClient.GetAuditLogAsync), new List<AuditEntryDto>
            {
                new() { TimestampUtc = DateTime.UtcNow, UserName = "S. Abrahams", Action = "View", FileNumber = "EST-001", Target = "GET api/Estates/{estateId}", StatusCode = 200 }
            })
            .Returns(nameof(IApiClient.GetCorrespondenceAsync), new List<CorrespondenceDto>
            {
                new() { SentAtUtc = DateTime.UtcNow, Channel = "Letter", Kind = "Vehicle transfer letter", RecipientName = "Traffic Dept", Subject = "Transfer", Status = "Generated", DocumentId = Guid.NewGuid() }
            })
            .Returns(nameof(IApiClient.GetTwoFactorStatusAsync), new TwoFactorStatusDto { IsEnabled = false })
            .Returns(nameof(IApiClient.StartTwoFactorSetupAsync), new TwoFactorSetupResponse
            {
                SharedKey = "JBSWY3DPEHPK3PXP",
                AuthenticatorUri = "otpauth://totp/EstateFlow:test%40test.local?secret=JBSWY3DPEHPK3PXP&issuer=EstateFlow&digits=6"
            });

    public static IAuthState Admin() =>
        new AuthState { Token = "token", FullName = "Mohamed Ahmed", Role = "FirmAdministrator" };
}

// Every screen must load (no missing styles or resources) and every binding
// must resolve (no misspelt property names), with realistic data in place.
[Collection("WPF")]
public class ScreenTests
{
    private static INavigationService Nav() => Fake<INavigationService>.Create().Object;

    private static void Render(FrameworkElement view, object dataContext)
    {
        view.DataContext = dataContext;
        Wpf.DoEvents();

        view.Measure(new Size(1280, 900));
        view.Arrange(new Rect(0, 0, 1280, 900));
        view.UpdateLayout();

        // Show every tab so the content of each one is built and bound.
        foreach (var tabs in Descendants<TabControl>(view).ToList())
        {
            for (var i = 0; i < tabs.Items.Count; i++)
            {
                tabs.SelectedIndex = i;
                view.UpdateLayout();
                Wpf.DoEvents();
            }
        }
    }

    private static IEnumerable<T> Descendants<T>(DependencyObject root) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T match) yield return match;
            foreach (var nested in Descendants<T>(child)) yield return nested;
        }
    }

    private static void AssertNoBindingErrors()
    {
        var errors = Wpf.BindingErrors.Errors
            .Where(e => e.Contains("BindingExpression", StringComparison.Ordinal))
            .ToList();

        Assert.True(errors.Count == 0, "Binding errors:\n" + string.Join("\n", errors));
    }

    private static void Check(Func<(FrameworkElement View, object ViewModel)> build) =>
        Wpf.Run(() =>
        {
            Wpf.BindingErrors.Clear();
            var (view, viewModel) = build();
            Render(view, viewModel);
            AssertNoBindingErrors();
        });

    [Fact]
    public void Estate_screen_with_all_tabs_loads_and_binds()
    {
        Check(() =>
        {
            var vm = new EstateDetailViewModel(SampleData.Api().Object, Nav());
            vm.Estate = SampleData.Estate();
            return (new EstateDetailView(), vm);
        });
    }

    [Fact]
    public void Dashboard_loads_and_binds()
    {
        Check(() => (new DashboardView(), new DashboardViewModel(SampleData.Api().Object, Nav())));
    }

    [Fact]
    public void Admin_screen_loads_and_binds()
    {
        Check(() =>
        {
            var vm = new AdminViewModel(SampleData.Api().Object);
            vm.LoadAsync().GetAwaiter();
            return (new AdminView(), vm);
        });
    }

    [Fact]
    public void Security_screen_with_qr_code_loads_and_binds()
    {
        Check(() =>
        {
            var vm = new SecurityViewModel(SampleData.Api().Object, SampleData.Admin());
            vm.StartSetupCommand.Execute(null);
            return (new SecurityView(), vm);
        });
    }

    [Fact]
    public void Sidebar_shows_the_signed_in_user()
    {
        Wpf.Run(() =>
        {
            Wpf.BindingErrors.Clear();

            var shell = new ShellViewModel(new ServiceCollection().BuildServiceProvider(), SampleData.Admin());
            var sidebar = new SidebarView();
            Render(sidebar, shell);

            AssertNoBindingErrors();
            Assert.Equal("Mohamed Ahmed", shell.UserDisplayName);
            Assert.Equal("Firm Administrator", shell.UserRoleLabel);
            Assert.Equal("MA", shell.UserInitials);
            Assert.True(shell.IsAdministrator);
        });
    }

    [Fact]
    public void Other_screens_load_and_bind()
    {
        Check(() => (new LoginView(), new LoginViewModel(Nav(), SampleData.Api().Object, new AuthState())));
        Check(() => (new DiaryView(), new DiaryViewModel(SampleData.Api().Object)));
        Check(() => (new ReportsView(), new ReportsViewModel(SampleData.Api().Object)));
        Check(() => (new DocumentScanView(), new DocumentScanViewModel(SampleData.Api().Object)));
        Check(() =>
        {
            var vm = new EstateFormViewModel(SampleData.Api().Object, Nav());
            vm.InitializeForEditAsync(SampleData.EstateId).GetAwaiter();
            return (new EstateFormView(), vm);
        });
    }
}

[CollectionDefinition("WPF", DisableParallelization = true)]
public class WpfCollection { }
