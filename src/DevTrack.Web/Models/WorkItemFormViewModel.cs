using System.ComponentModel.DataAnnotations;
using DevTrack.Core.Entities;

namespace DevTrack.Web.Models;

public sealed class WorkItemFormViewModel
{
    public int Id { get; set; }
    [Required, StringLength(300)] public string Title { get; set; } = string.Empty;
    [StringLength(2_000)] public string? Description { get; set; }
    [EnumDataType(typeof(WorkItemStatus))] public WorkItemStatus Status { get; set; }
    [StringLength(256)] public string? AssignedTo { get; set; }
    [Range(0, int.MaxValue)] public int EstimatedHours { get; set; }
    [Range(1, int.MaxValue)] public int SprintId { get; set; }
}
