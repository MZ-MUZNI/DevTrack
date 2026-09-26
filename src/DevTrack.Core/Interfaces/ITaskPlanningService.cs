using DevTrack.Core.AI;

namespace DevTrack.Core.Interfaces;

public interface ITaskPlanningService
{
    Task<TaskPlanningResult> SuggestSubtasksAsync(TaskPlanningRequest request, CancellationToken cancellationToken = default);
}
