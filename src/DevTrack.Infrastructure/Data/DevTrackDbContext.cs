using DevTrack.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace DevTrack.Infrastructure.Data;

// This class IS the bridge between your C# classes and actual SQL tables.
// EF Core reads this class + your entity classes and generates the schema.
public class DevTrackDbContext : Microsoft.EntityFrameworkCore.DbContext
{
    // This constructor takes DbContextOptions, which carries the connection
    // string. It gets filled in by Dependency Injection in Program.cs —
    // you will NEVER "new DevTrackDbContext()" by hand in a Controller.
    public DevTrackDbContext(DbContextOptions<DevTrackDbContext> options)
        : base(options)
    {
    }

    // Each DbSet<T> becomes a table.
    // DbSet<ProjectEntity> -> "ProjectEntities" table (by default naming convention)
    public DbSet<ProjectEntity> Projects => Set<ProjectEntity>();
    public DbSet<Sprint> Sprints => Set<Sprint>();
    public DbSet<WorkItem> WorkItems => Set<WorkItem>();

    // OnModelCreating is where you fine-tune what EF Core inferred
    // automatically. We didn't NEED anything here for this simple model
    // (EF Core figures out the Project -> Sprint -> WorkItem relationships
    // from the FK + navigation properties alone), but this is where things
    // like unique constraints, max string lengths, or renaming tables go.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ProjectEntity>()
            .Property(p => p.Name)
            .HasMaxLength(200)
            .IsRequired();

        modelBuilder.Entity<WorkItem>()
            .Property(w => w.Title)
            .HasMaxLength(300)
            .IsRequired();
    }
}
