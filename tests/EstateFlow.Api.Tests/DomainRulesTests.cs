using EstateFlow.Core.Models;
using EstateFlow.Core.Validation;
using EstateFlow.Core.Workflow;
using EstateFlow.Data.Context;
using EstateFlow.Data.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;

namespace EstateFlow.Api.Tests;

public class SouthAfricanIdNumberTests
{
    [Theory]
    [InlineData("8001015009087")]
    [InlineData("800101 5009 087")]
    public void Valid_id_is_accepted_with_date_of_birth(string id)
    {
        var check = SouthAfricanIdNumber.Check(id);

        Assert.True(check.IsAcceptable);
        Assert.True(check.IsSouthAfricanId);
        Assert.Equal("8001015009087", check.Normalized);
        Assert.Equal(new DateTime(1980, 1, 1), check.DateOfBirth);
    }

    [Theory]
    [InlineData("8001015009088", "check digit")]   // one digit wrong
    [InlineData("8013015009087", "date of birth")]  // month 13
    [InlineData("800101500908", "13 digits")]       // digit missing
    [InlineData("8001015009387", "11th digit")]     // citizenship digit 3
    public void Typos_are_rejected_with_a_helpful_message(string id, string expectedHint)
    {
        var check = SouthAfricanIdNumber.Check(id);

        Assert.False(check.IsAcceptable);
        Assert.Contains(expectedHint, check.Error);
    }

    [Fact]
    public void Passport_numbers_are_accepted()
    {
        var check = SouthAfricanIdNumber.Check("a12345678");

        Assert.True(check.IsAcceptable);
        Assert.False(check.IsSouthAfricanId);
        Assert.Equal("A12345678", check.Normalized);
    }

    [Fact]
    public void Empty_is_allowed()
    {
        Assert.True(SouthAfricanIdNumber.Check("  ").IsAcceptable);
    }
}

public class MasterProcessTests
{
    private static readonly DateTime DateOfDeath = new(2026, 1, 10);

    private static Dictionary<EstateMilestoneStage, DateTime> Done(params (EstateMilestoneStage Stage, DateTime On)[] items) =>
        items.ToDictionary(i => i.Stage, i => i.On);

    [Fact]
    public void New_estate_must_be_reported_within_14_days_of_death()
    {
        var progress = MasterProcess.Evaluate(DateOfDeath, Done(), new DateTime(2026, 1, 15));

        var report = progress.Milestones[0];
        Assert.Equal(EstateMilestoneStage.ReportedToMaster, report.Stage);
        Assert.Equal(new DateTime(2026, 1, 24), report.DueDate);
        Assert.True(report.IsStatutoryDeadline);
        Assert.True(report.IsNext);
        Assert.True(report.IsDueSoon);
        Assert.False(progress.NeedsAttention);
        Assert.Equal(0, progress.PercentComplete);
    }

    [Fact]
    public void Missed_statutory_deadline_is_flagged_with_days_overdue()
    {
        var progress = MasterProcess.Evaluate(DateOfDeath, Done(), new DateTime(2026, 2, 3));

        Assert.True(progress.NeedsAttention);
        Assert.Equal(10, progress.DaysOverdue);
        Assert.Contains("statutory deadline", progress.AttentionReason);
    }

    [Fact]
    public void Account_is_due_six_months_after_letters_of_executorship()
    {
        var progress = MasterProcess.Evaluate(
            DateOfDeath,
            Done(
                (EstateMilestoneStage.ReportedToMaster, new DateTime(2026, 1, 20)),
                (EstateMilestoneStage.LettersOfExecutorship, new DateTime(2026, 3, 5))),
            new DateTime(2026, 3, 10));

        var account = progress.Milestones.Single(m => m.Stage == EstateMilestoneStage.AccountLodged);
        Assert.Equal(new DateTime(2026, 9, 5), account.DueDate);
        Assert.True(account.IsStatutoryDeadline);
        Assert.Equal(EstateMilestoneStage.CreditorsAdvertised, progress.NextMilestone!.Stage);
        Assert.Equal(29, progress.PercentComplete); // 2 of 7
    }

    [Fact]
    public void Earliest_lodging_date_is_after_the_creditor_claim_period()
    {
        var earliest = MasterProcess.EarliestAccountLodgingDate(
            Done((EstateMilestoneStage.CreditorsAdvertised, new DateTime(2026, 4, 1))));

        Assert.Equal(new DateTime(2026, 5, 1), earliest);
    }

    [Fact]
    public void Stalled_estate_is_flagged_when_nothing_is_overdue()
    {
        // Letters received but no follow-up deadline configured for 120 days.
        var options = new MasterProcessOptions { AdvertiseWithinDaysOfLetters = 365 };

        var progress = MasterProcess.Evaluate(
            DateOfDeath,
            Done(
                (EstateMilestoneStage.ReportedToMaster, new DateTime(2026, 1, 20)),
                (EstateMilestoneStage.LettersOfExecutorship, new DateTime(2026, 2, 1))),
            new DateTime(2026, 6, 1),
            options);

        Assert.True(progress.NeedsAttention);
        Assert.Contains("No stage completed", progress.AttentionReason);
    }

    [Fact]
    public void Finalised_estate_never_needs_attention()
    {
        var all = MasterProcess.Definitions.ToDictionary(d => d.Stage, _ => new DateTime(2026, 2, 1));

        var progress = MasterProcess.Evaluate(DateOfDeath, all, new DateTime(2030, 1, 1));

        Assert.True(progress.IsFinalised);
        Assert.False(progress.NeedsAttention);
        Assert.Equal(100, progress.PercentComplete);
        Assert.Equal(EstateStatuses.Finalised, MasterProcess.DeriveEstateStatus(all.Keys.ToList()));
    }

    [Theory]
    [InlineData("System.Windows.Controls.ComboBoxItem: In Progress", "In Progress")]
    [InlineData("finalised", "Finalised")]
    [InlineData("InProgress", "In Progress")]
    [InlineData("", "Reported")]
    public void Broken_status_values_are_normalised(string stored, string expected)
    {
        Assert.Equal(expected, EstateStatuses.Normalize(stored));
    }
}

public class FieldEncryptionTests
{
    private static readonly byte[] Key = Enumerable.Range(1, 32).Select(i => (byte)i).ToArray();

    [Fact]
    public void Protector_round_trips_and_passes_legacy_plaintext_through()
    {
        var protector = new AesGcmFieldProtector(Key);

        var stored = protector.Protect("8001015009087");

        Assert.StartsWith(AesGcmFieldProtector.Prefix, stored);
        Assert.DoesNotContain("8001015009087", stored);
        Assert.Equal("8001015009087", protector.Unprotect(stored));
        Assert.Equal("legacy-plain", protector.Unprotect("legacy-plain"));
        Assert.NotEqual(stored, protector.Protect("8001015009087")); // random nonce
    }

    // Uses SQLite (a real relational database) and reads the stored column
    // with plain SQL, so the check can't be fooled by EF's own conversion.
    [Fact]
    public async Task Id_numbers_are_encrypted_in_the_database_but_readable_through_the_app()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var protector = new AesGcmFieldProtector(Key);
        var options = new DbContextOptionsBuilder<EstateFlowDbContext>().UseSqlite(connection).Options;
        var estateId = Guid.NewGuid();

        await using (var app = new EstateFlowDbContext(options, protector))
        {
            await app.Database.EnsureCreatedAsync();

            var controller = new EstateController { StaffName = "Controller", Email = "c@test.local" };
            app.Controllers.Add(controller);
            app.Estates.Add(new Estate
            {
                EstateId = estateId,
                FileNumber = "ENC-1",
                ControllerId = controller.ControllerId,
                Deceased = new Deceased { FullName = "Test", IdNumber = "8001015009087" }
            });
            await app.SaveChangesAsync();
        }

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT IdNumber FROM Deceased";
            var stored = (string)(await command.ExecuteScalarAsync())!;

            Assert.StartsWith(AesGcmFieldProtector.Prefix, stored);
            Assert.DoesNotContain("8001015009087", stored);
        }

        await using (var app = new EstateFlowDbContext(options, protector))
        {
            var read = await app.Deceased.SingleAsync(d => d.EstateId == estateId);
            Assert.Equal("8001015009087", read.IdNumber);
        }
    }
}
