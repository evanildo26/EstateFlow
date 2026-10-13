namespace EstateFlow.Core.Models;

public class Estate
{
    public Guid EstateId { get; set; } = Guid.NewGuid();
    public string FileNumber { get; set; } = string.Empty;
    public string? MastersReference { get; set; }
    public DateTime DateOfDeath { get; set; }
    public string EstateStatus { get; set; } = "Reported"; // matches state diagram start
    public bool IsDutiable { get; set; }

    // FK — the controller assigned to this estate
    public Guid ControllerId { get; set; }
    public EstateController? Controller { get; set; }

    // Navigation collections — one-to-many from Estate
    public Deceased? Deceased { get; set; }
    public ICollection<Executor> Executors { get; set; } = new List<Executor>();
    public ICollection<Beneficiary> Beneficiaries { get; set; } = new List<Beneficiary>();
    public ICollection<ImmovableProperty> ImmovableProperties { get; set; } = new List<ImmovableProperty>();
    public ICollection<MovableAsset> MovableAssets { get; set; } = new List<MovableAsset>();
    public ICollection<FinancialClaim> FinancialClaims { get; set; } = new List<FinancialClaim>();
    public ICollection<Liability> Liabilities { get; set; } = new List<Liability>();
    public ICollection<DiaryEntry> DiaryEntries { get; set; } = new List<DiaryEntry>();
    public ICollection<ProgressStage> ProgressStages { get; set; } = new List<ProgressStage>();
    public ICollection<ScannedDocument> ScannedDocuments { get; set; } = new List<ScannedDocument>();
    public ICollection<AutomatedReport> AutomatedReports { get; set; } = new List<AutomatedReport>();
    public ICollection<EstateMilestone> Milestones { get; set; } = new List<EstateMilestone>();
    public ICollection<CorrespondenceRecord> Correspondence { get; set; } = new List<CorrespondenceRecord>();
}