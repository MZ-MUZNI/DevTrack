using System.ComponentModel.DataAnnotations;

namespace DevTrack.Api.Contracts;

public sealed class TaskSuggestionRequest
{
    [Required, StringLength(300)]
    public string Title { get; init; } = string.Empty;

    [StringLength(4_000)]
    public string? Description { get; init; }

    [Range(1, int.MaxValue)]
    public int SprintId { get; init; }
}

public sealed record TaskSuggestionResponse(
    string Summary,
    IReadOnlyList<string> Assumptions,
    IReadOnlyList<SuggestedSubtaskResponse> Subtasks);

public sealed record SuggestedSubtaskResponse(string Title, string Description, int EstimatedHours);
