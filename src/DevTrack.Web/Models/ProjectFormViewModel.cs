using System.ComponentModel.DataAnnotations;

namespace DevTrack.Web.Models;

public sealed class ProjectFormViewModel
{
    public int Id { get; set; }

    [Required, StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(4_000)]
    public string? Description { get; set; }
}
