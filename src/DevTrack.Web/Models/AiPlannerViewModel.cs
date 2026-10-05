using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DevTrack.Web.Models;

public sealed class AiPlannerViewModel
{
    [Required, StringLength(300)]
    [Display(Name = "What needs to be done?")]
    public string Title { get; set; } = string.Empty;

    [StringLength(4_000)]
    [Display(Name = "Context for the assistant")]
    public string? Description { get; set; }

    [Range(1, int.MaxValue)]
    [Display(Name = "Sprint")]
    public int SprintId { get; set; }

    public IEnumerable<SelectListItem> Sprints { get; set; } = [];
    public string? Summary { get; set; }
    public IReadOnlyList<string> Assumptions { get; set; } = [];
    public List<AiSuggestedWorkItemViewModel> Suggestions { get; set; } = [];
}

public sealed class AiSuggestedWorkItemViewModel
{
    public bool Selected { get; set; } = true;

    [Required, StringLength(300)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(2_000)]
    public string Description { get; set; } = string.Empty;

    [Range(1, 40)]
    [Display(Name = "Hours")]
    public int EstimatedHours { get; set; }
}
