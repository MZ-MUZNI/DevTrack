using DevTrack.Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using DevTrack.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace DevTrack.Infrastructure.Data
{
    public class WorkItemRepository : Repository<WorkItem>, IWorkItemRepository
    {
        private readonly DevTrackDbContext _context;

        public WorkItemRepository(DevTrackDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IEnumerable<WorkItem>> GetAllWithSprintAsync() 
        {
            return await _context.WorkItems
                .Include(w => w.Sprint)
                .ToListAsync();
        }

        public async Task<WorkItem?> GetByIdWithSprintAsync(int id)
        {
            return await _context.WorkItems
                .Include(w => w.Sprint)
                .FirstOrDefaultAsync(w => w.Id == id);
        }
    }
}
