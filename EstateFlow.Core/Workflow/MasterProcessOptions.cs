namespace EstateFlow.Core.Workflow;

// Deadline periods used for the stage checklist. Bound from the "Workflow"
// configuration section, so the firm can change them without a code change.
//
// The statutory periods come from the Administration of Estates Act 66 of 1965.
// The others are firm-practice follow-up periods. CONFIRM ALL VALUES WITH STBB
// before relying on them.
public class MasterProcessOptions
{
    public const string SectionName = "Workflow";

    // s7 / s9: death notice and inventory within 14 days of death. (Statutory)
    public int ReportWithinDaysOfDeath { get; set; } = 14;

    // Follow up with the Master if Letters of Executorship haven't arrived. (Firm practice)
    public int LettersFollowUpDaysAfterReport { get; set; } = 60;

    // Advertise for creditors soon after Letters are issued. (Firm practice)
    public int AdvertiseWithinDaysOfLetters { get; set; } = 30;

    // s29: creditors get at least 30 days from the advert to lodge claims. (Statutory)
    public int CreditorClaimPeriodDays { get; set; } = 30;

    // s35(1): liquidation & distribution account within 6 months of Letters. (Statutory)
    public int AccountWithinMonthsOfLetters { get; set; } = 6;

    // Master's examination + s35(4) 21-day inspection period. (Firm practice estimate)
    public int InspectionFollowUpDaysAfterLodging { get; set; } = 90;

    // s35(12): pay out "forthwith" after the inspection period. (Firm practice)
    public int PayOutWithinDaysOfInspection { get; set; } = 14;

    // Lodge receipts/vouchers with the Master to finalise. (Firm practice)
    public int FinaliseWithinDaysOfPayout { get; set; } = 30;

    // Flag an estate when no stage has been completed for this long.
    public int StalledAfterDays { get; set; } = 90;

    // Show "due soon" this many days before a deadline.
    public int DueSoonDays { get; set; } = 14;
}
