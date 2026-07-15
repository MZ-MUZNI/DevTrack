namespace DevTrack.Core.Entities;

public class Sprint
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    // Foreign key + navigation property back to the parent Project.
    public int ProjectId { get; set; }
    public ProjectEntity? Project { get; set; }

    public ICollection<WorkItem> WorkItems { get; set; } = new List<WorkItem>();
}
