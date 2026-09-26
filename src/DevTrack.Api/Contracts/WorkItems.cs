using System.ComponentModel.DataAnnotations;
using DevTrack.Core.Entities;

namespace DevTrack.Api.Contracts;

public sealed class WorkItemRequest
{
    [Required, StringLength(300)]
    public string Title { get; init; } = string.Empty;

    public string? Description { get; init; }
    [EnumDataType(typeof(WorkItemStatus))]
    public WorkItemStatus Status { get; init; } = WorkItemStatus.Backlog;
    public string? AssignedTo { get; init; }

    [Range(0, int.MaxValue)]
    public int EstimatedHours { get; init; }

    [Range(1, int.MaxValue)]
    public int SprintId { get; init; }
}

public sealed record WorkItemResponse(
    int Id,
    string Title,
    string? Description,
    WorkItemStatus Status,
    string? AssignedTo,
    int EstimatedHours,
    int SprintId,
    string? SprintName);
