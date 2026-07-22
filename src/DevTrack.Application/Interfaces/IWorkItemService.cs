using System;
using System.Collections.Generic;
using System.Text;
using DevTrack.Core.Entities;

namespace DevTrack.Application.Interfaces
{
    public interface IWorkItemService
    {
        Task<IEnumerable<WorkItem>> GetAllAsync();
        Task<WorkItem?> GetByIdAsync(int id);
        Task CreateAsync(WorkItem workItem);
        Task UpdateAsync(WorkItem workItem);
        Task DeleteAsync(int id);
    }
}
