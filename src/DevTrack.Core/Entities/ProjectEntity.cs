namespace DevTrack.Core.Entities;

// Named "ProjectEntity" (not "Project") to avoid any confusion with
// MSBuild/.csproj "Project" concepts elsewhere in the solution.
public class ProjectEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property: one Project has many Sprints.
    // EF Core uses this to build the foreign key relationship.
    public ICollection<Sprint> Sprints { get; set; } = new List<Sprint>();
}
