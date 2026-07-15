using DevTrack.Core.Entities;

namespace DevTrack.Core.Interfaces;

// Extends the generic repository instead of replacing it - Sprint still
// gets GetByIdAsync, AddAsync, etc "for free" from IRepository<Sprint>.
// We only add what's genuinely Sprint-specific here.
public interface ISprintRepository : IRepository<Sprint>
{
    Task<IEnumerable<Sprint>> GetAllWithProjectAsync();
}
