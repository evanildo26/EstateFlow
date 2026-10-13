namespace EstateFlow.Core.Dtos;

public class EstateSummaryDto
{
    public Guid EstateId { get; set; }
    public string FileNumber { get; set; } = string.Empty;
    public string? DeceasedFullName { get; set; }
    public string EstateStatus { get; set; } = string.Empty;
    public DateTime DateOfDeath { get; set; }
    public string? ControllerName { get; set; }
    public int OpenDiaryCount { get; set; }
    public int OverdueDiaryCount { get; set; }
    public DateTime? NextDiaryDue { get; set; }

    // From the Master's-process checklist.
    public int ProgressPercent { get; set; }
    public string? NextMilestoneTitle { get; set; }
    public DateTime? NextDeadline { get; set; }
    public bool NeedsAttention { get; set; }
    public string? AttentionReason { get; set; }
    public int DaysOverdue { get; set; }
}