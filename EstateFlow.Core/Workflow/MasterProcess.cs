namespace EstateFlow.Core.Workflow;

public sealed record MilestoneDefinition(
    EstateMilestoneStage Stage,
    string Title,
    string Description);

public sealed record MilestoneState(
    EstateMilestoneStage Stage,
    string Title,
    string Description,
    DateTime? CompletedOn,
    DateTime? DueDate,
    bool IsStatutoryDeadline,
    string? DeadlineBasis,
    bool IsOverdue,
    bool IsDueSoon,
    bool IsNext);

public sealed record EstateProgress(
    IReadOnlyList<MilestoneState> Milestones,
    int CompletedCount,
    int PercentComplete,
    MilestoneState? NextMilestone,
    bool IsFinalised,
    bool NeedsAttention,
    string? AttentionReason,
    int DaysOverdue);

// Works out where an estate is in the Master's process and what is due next.
// Pure and deterministic ("today" is passed in) so it can be unit tested.
public static class MasterProcess
{
    public static IReadOnlyList<MilestoneDefinition> Definitions { get; } = new[]
    {
        new MilestoneDefinition(EstateMilestoneStage.ReportedToMaster,
            "Estate reported to the Master",
            "Death notice, inventory and supporting documents lodged with the Master of the High Court."),
        new MilestoneDefinition(EstateMilestoneStage.LettersOfExecutorship,
            "Letters of Executorship issued",
            "The Master has appointed the executor (or issued Letters of Authority for smaller estates)."),
        new MilestoneDefinition(EstateMilestoneStage.CreditorsAdvertised,
            "Advertised for creditors",
            "Notice to creditors published in the Government Gazette and a local newspaper (s29)."),
        new MilestoneDefinition(EstateMilestoneStage.AccountLodged,
            "Liquidation & distribution account lodged",
            "Final account lodged with the Master for examination (s35)."),
        new MilestoneDefinition(EstateMilestoneStage.InspectionCompleted,
            "Inspection period completed",
            "Account advertised and lay open for inspection for 21 days without objections (s35(4))."),
        new MilestoneDefinition(EstateMilestoneStage.PaidOut,
            "Heirs and creditors paid",
            "Distribution made in accordance with the confirmed account (s35(12))."),
        new MilestoneDefinition(EstateMilestoneStage.Finalised,
            "Estate finalised",
            "Receipts and vouchers lodged; the Master has issued the filing slip."),
    };

    public static MilestoneDefinition Get(EstateMilestoneStage stage) =>
        Definitions.First(d => d.Stage == stage);

    public static EstateProgress Evaluate(
        DateTime dateOfDeath,
        IReadOnlyDictionary<EstateMilestoneStage, DateTime> completed,
        DateTime today,
        MasterProcessOptions? options = null)
    {
        options ??= new MasterProcessOptions();
        today = today.Date;

        DateTime? Done(EstateMilestoneStage stage) =>
            completed.TryGetValue(stage, out var date) ? date.Date : null;

        EstateMilestoneStage? nextStage = null;
        foreach (var definition in Definitions)
        {
            if (Done(definition.Stage) is null)
            {
                nextStage = definition.Stage;
                break;
            }
        }

        var states = new List<MilestoneState>();

        foreach (var definition in Definitions)
        {
            var (due, statutory, basis) = DueDateFor(definition.Stage, dateOfDeath.Date, Done, options);
            var completedOn = Done(definition.Stage);
            var isOpen = completedOn is null;

            states.Add(new MilestoneState(
                definition.Stage,
                definition.Title,
                definition.Description,
                completedOn,
                due,
                statutory,
                basis,
                IsOverdue: isOpen && due is not null && due.Value < today,
                IsDueSoon: isOpen && due is not null && due.Value >= today &&
                           (due.Value - today).TotalDays <= options.DueSoonDays,
                IsNext: definition.Stage == nextStage));
        }

        var completedCount = states.Count(s => s.CompletedOn is not null);
        var isFinalised = Done(EstateMilestoneStage.Finalised) is not null;
        var next = states.FirstOrDefault(s => s.IsNext);

        string? attention = null;
        var daysOverdue = 0;

        if (!isFinalised)
        {
            var overdue = states
                .Where(s => s.IsOverdue)
                .OrderBy(s => s.DueDate)
                .FirstOrDefault();

            if (overdue is not null)
            {
                daysOverdue = (int)(today - overdue.DueDate!.Value).TotalDays;
                var kind = overdue.IsStatutoryDeadline ? "statutory deadline" : "follow-up date";
                var plural = daysOverdue == 1 ? string.Empty : "s";
                attention = $"{overdue.Title}: {kind} passed {daysOverdue} day{plural} ago ({overdue.DueDate:dd MMM yyyy}).";
            }
            else
            {
                var lastActivity = completed.Count == 0
                    ? dateOfDeath.Date
                    : completed.Values.Max().Date;

                var idleDays = (int)(today - lastActivity).TotalDays;

                if (idleDays > options.StalledAfterDays)
                {
                    attention = $"No stage completed for {idleDays} days.";
                }
            }
        }

        return new EstateProgress(
            states,
            completedCount,
            (int)Math.Round(completedCount * 100.0 / Definitions.Count),
            next,
            isFinalised,
            attention is not null,
            attention,
            daysOverdue);
    }

    // The earliest date the account may be lodged: the creditor claim period
    // must have run out. Null until creditors have been advertised.
    public static DateTime? EarliestAccountLodgingDate(
        IReadOnlyDictionary<EstateMilestoneStage, DateTime> completed,
        MasterProcessOptions? options = null)
    {
        options ??= new MasterProcessOptions();

        return completed.TryGetValue(EstateMilestoneStage.CreditorsAdvertised, out var advertised)
            ? advertised.Date.AddDays(options.CreditorClaimPeriodDays)
            : null;
    }

    // The single status shown on lists, derived from the checklist.
    public static string DeriveEstateStatus(IReadOnlyCollection<EstateMilestoneStage> completed)
    {
        if (completed.Contains(EstateMilestoneStage.Finalised))
            return EstateStatuses.Finalised;

        return completed.Count == 0
            ? EstateStatuses.Reported
            : EstateStatuses.InProgress;
    }

    private static (DateTime? Due, bool Statutory, string? Basis) DueDateFor(
        EstateMilestoneStage stage,
        DateTime dateOfDeath,
        Func<EstateMilestoneStage, DateTime?> done,
        MasterProcessOptions o)
    {
        switch (stage)
        {
            case EstateMilestoneStage.ReportedToMaster:
                return (dateOfDeath.AddDays(o.ReportWithinDaysOfDeath), true,
                    $"{o.ReportWithinDaysOfDeath} days after death (s7/s9)");

            case EstateMilestoneStage.LettersOfExecutorship:
                return (done(EstateMilestoneStage.ReportedToMaster)?.AddDays(o.LettersFollowUpDaysAfterReport), false,
                    $"Follow up with the Master {o.LettersFollowUpDaysAfterReport} days after reporting");

            case EstateMilestoneStage.CreditorsAdvertised:
                return (done(EstateMilestoneStage.LettersOfExecutorship)?.AddDays(o.AdvertiseWithinDaysOfLetters), false,
                    $"{o.AdvertiseWithinDaysOfLetters} days after Letters of Executorship");

            case EstateMilestoneStage.AccountLodged:
                return (done(EstateMilestoneStage.LettersOfExecutorship)?.AddMonths(o.AccountWithinMonthsOfLetters), true,
                    $"{o.AccountWithinMonthsOfLetters} months after Letters of Executorship (s35(1)), " +
                    $"and not before the {o.CreditorClaimPeriodDays}-day creditor claim period ends");

            case EstateMilestoneStage.InspectionCompleted:
                return (done(EstateMilestoneStage.AccountLodged)?.AddDays(o.InspectionFollowUpDaysAfterLodging), false,
                    $"Follow up {o.InspectionFollowUpDaysAfterLodging} days after lodging the account");

            case EstateMilestoneStage.PaidOut:
                return (done(EstateMilestoneStage.InspectionCompleted)?.AddDays(o.PayOutWithinDaysOfInspection), false,
                    $"{o.PayOutWithinDaysOfInspection} days after the inspection period (s35(12))");

            case EstateMilestoneStage.Finalised:
                return (done(EstateMilestoneStage.PaidOut)?.AddDays(o.FinaliseWithinDaysOfPayout), false,
                    $"{o.FinaliseWithinDaysOfPayout} days after paying out");

            default:
                return (null, false, null);
        }
    }
}
