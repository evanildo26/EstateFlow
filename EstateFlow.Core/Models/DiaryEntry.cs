using EstateFlow.Core.Workflow;
namespace EstateFlow.Core.Models;

public class DiaryEntry
{
    public Guid DiaryId { get; set; } = Guid.NewGuid();
    public Guid EstateId { get; set; }
    public Estate? Estate { get; set; }

    public DateTime DueDate { get; set; }
    public string Note { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public NotifyMethod NotifyBy { get; set; } = NotifyMethod.Email;
}