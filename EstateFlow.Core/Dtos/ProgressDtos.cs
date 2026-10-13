namespace EstateFlow.Core.Dtos;

public class EstateProgressDto
{
    public int PercentComplete { get; set; }
    public int CompletedCount { get; set; }
    public int TotalCount { get; set; }
    public bool IsFinalised { get; set; }
    public bool NeedsAttention { get; set; }
    public string? AttentionReason { get; set; }
    public int DaysOverdue { get; set; }
    public string? NextMilestoneTitle { get; set; }
    public DateTime? NextDeadline { get; set; }
    public DateTime? EarliestAccountLodgingDate { get; set; }
    public List<MilestoneDto> Milestones { get; set; } = new();
}

public class MilestoneDto
{
    // EstateMilestoneStage name, e.g. "LettersOfExecutorship".
    public string Stage { get; set; } = string.Empty;
    public int Order { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime? CompletedOn { get; set; }
    public string? Notes { get; set; }
    public DateTime? DueDate { get; set; }
    public bool IsStatutoryDeadline { get; set; }
    public string? DeadlineBasis { get; set; }
    public bool IsOverdue { get; set; }
    public bool IsDueSoon { get; set; }
    public bool IsNext { get; set; }
    public bool IsComplete => CompletedOn.HasValue;
}

public class CompleteMilestoneRequest
{
    public DateTime CompletedOn { get; set; }
    public string? Notes { get; set; }
}
