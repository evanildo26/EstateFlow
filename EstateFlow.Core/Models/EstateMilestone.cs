using EstateFlow.Core.Workflow;

namespace EstateFlow.Core.Models;

// A completed stage of the Master's process (see MasterProcess).
// A row exists only once the stage is done.
public class EstateMilestone
{
    public Guid MilestoneId { get; set; } = Guid.NewGuid();
    public Guid EstateId { get; set; }
    public Estate? Estate { get; set; }

    public EstateMilestoneStage Stage { get; set; }
    public DateTime CompletedOn { get; set; }
    public string? Notes { get; set; }

    public string RecordedBy { get; set; } = string.Empty;
    public DateTime RecordedAtUtc { get; set; } = DateTime.UtcNow;
}
