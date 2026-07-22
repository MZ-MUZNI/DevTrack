using System;
using System.Collections.Generic;
using System.Text;
using DevTrack.Application.Interfaces;
using DevTrack.Core.Entities;
using DevTrack.Core.Interfaces;

namespace DevTrack.Application.Services
{
    public class WorkItemService : IWorkItemService
    {
        private readonly IWorkItemRepository _workItemRepository;
        private readonly ISprintRepository _sprintRepository;

        public WorkItemService(
            IWorkItemRepository workItemRepository, 
            ISprintRepository sprintRepository)
        {
            _workItemRepository = workItemRepository;
            _sprintRepository = sprintRepository;
        }

        public async Task<IEnumerable<WorkItem>> GetAllAsync()
        {
            return await _workItemRepository.GetAllWithSprintAsync();
        }

        public async Task<WorkItem?> GetByIdAsync(int id)
        {
            return await _workItemRepository.GetByIdWithSprintAsync(id);
        }

        public async Task CreateAsync(WorkItem workItem)
        {
            // Business rule: can't add a WorkItem to a Sprint that has already ended.
            var sprint = await _sprintRepository.GetByIdAsync(workItem.SprintId);

            if (sprint == null)
            {
                throw new InvalidOperationException(
                    $"Sprint with Id {workItem.SprintId} does not exist.");
            }

            if (sprint.EndDate < DateTime.Now)
            {
                throw new InvalidOperationException(
                    $"Cannot add a work item to sprint '{sprint.Name}' because it has already ended.");
            }

            await _workItemRepository.AddAsync(workItem);
            await _workItemRepository.SaveChangesAsync();
        }

        public async Task UpdateAsync(WorkItem workItem)
        {
            // Same rule applies on update — someone could try to move
            // a WorkItem into a closed Sprint via Edit too.
            var sprint = await _sprintRepository.GetByIdAsync(workItem.SprintId);

            if (sprint == null)
            {
                throw new InvalidOperationException(
                    $"Sprint with Id {workItem.SprintId} does not exist.");
            }

            if (sprint.EndDate < DateTime.Now)
            {
                throw new InvalidOperationException(
                    $"Cannot move a work item into sprint '{sprint.Name}' because it has already ended.");
            }

            _workItemRepository.Update(workItem);
            await _workItemRepository.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var workItem = await _workItemRepository.GetByIdAsync(id);
            if (workItem == null) return;

            _workItemRepository.Delete(workItem);
            await _workItemRepository.SaveChangesAsync();
        }
    }
}
