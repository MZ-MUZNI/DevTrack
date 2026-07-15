namespace DevTrack.Core.Entities;

public enum WorkItemStatus
{
    Backlog,
    InProgress,
    InReview,
    Done
}

// Named "WorkItem" instead of "Task" on purpose: System.Threading.Tasks.Task
// is used everywhere for async code, and shadowing that name causes constant
// confusion and accidental `using` collisions. Good habit to carry forward.
public class WorkItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public WorkItemStatus Status { get; set; } = WorkItemStatus.Backlog;
    public string? AssignedTo { get; set; }
    public int EstimatedHours { get; set; }

    public int SprintId { get; set; }
    public Sprint? Sprint { get; set; }
}
