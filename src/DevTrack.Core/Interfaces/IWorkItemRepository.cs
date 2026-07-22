using DevTrack.Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DevTrack.Core.Interfaces
{
    public interface IWorkItemRepository : IRepository<WorkItem>
    {
        Task<IEnumerable<WorkItem>> GetAllWithSprintAsync();
        Task<WorkItem?> GetByIdWithSprintAsync(int id);
    }
}
