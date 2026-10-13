namespace EstateFlow.Core.Workflow;

// The standard stages of winding up a deceased estate through the Master of
// the High Court (Administration of Estates Act 66 of 1965), in order.
// Stored by name, so the numeric values can change safely.
public enum EstateMilestoneStage
{
    ReportedToMaster = 1,
    LettersOfExecutorship = 2,
    CreditorsAdvertised = 3,
    AccountLodged = 4,
    InspectionCompleted = 5,
    PaidOut = 6,
    Finalised = 7
}
