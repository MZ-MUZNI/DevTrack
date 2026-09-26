namespace DevTrack.Core.AI;

public sealed record TaskPlanningRequest(
    string Title,
    string? Description,
    DateTime SprintStartDate,
    DateTime SprintEndDate);

public sealed record TaskPlanningResult(
    string Summary,
    IReadOnlyList<string> Assumptions,
    IReadOnlyList<SuggestedSubtask> Subtasks);

public sealed record SuggestedSubtask(
    string Title,
    string Description,
    int EstimatedHours);
