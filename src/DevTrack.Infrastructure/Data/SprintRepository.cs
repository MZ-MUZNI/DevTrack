using DevTrack.Core.Entities;
using DevTrack.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DevTrack.Infrastructure.Data;

// Inherits from Repository<Sprint>, so it gets GetByIdAsync/AddAsync/etc
// automatically, and only needs to implement the one new method.
public class SprintRepository : Repository<Sprint>, ISprintRepository
{
    private readonly DevTrackDbContext _context;

    public SprintRepository(DevTrackDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Sprint>> GetAllWithProjectAsync()
    {
        // .Include(s => s.Project) tells EF Core: "when you generate the SQL,
        // add a JOIN to Projects and populate the Project navigation property
        // on each Sprint." This is the standard fix for the null-navigation
        // problem you just diagnosed.
        return await _context.Sprints
            .Include(s => s.Project)
            .ToListAsync();
    }
}
