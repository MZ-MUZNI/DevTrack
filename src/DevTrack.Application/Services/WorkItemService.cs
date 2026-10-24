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
        private readonly TimeProvider _timeProvider;
        private readonly TimeZoneInfo _businessTimeZone;

        public WorkItemService(
            IWorkItemRepository workItemRepository, 
            ISprintRepository sprintRepository,
            TimeProvider timeProvider,
            TimeZoneInfo businessTimeZone)
        {
            _workItemRepository = workItemRepository;
            _sprintRepository = sprintRepository;
            _timeProvider = timeProvider;
            _businessTimeZone = businessTimeZone;
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

            if (HasEnded(sprint))
            {
                throw new InvalidOperationException(
                    $"Cannot add a work item to sprint '{sprint.Name}' because it has already ended.");
            }

            await _workItemRepository.AddAsync(workItem);
            await _workItemRepository.SaveChangesAsync();
        }

        public async Task CreateManyAsync(IEnumerable<WorkItem> workItems)
        {
            var items = workItems.ToList();
            if (items.Count == 0) return;

            var sprintIds = items.Select(item => item.SprintId).Distinct().ToList();
            if (sprintIds.Count != 1)
                throw new InvalidOperationException("All work items in a batch must belong to the same sprint.");

            var sprint = await _sprintRepository.GetByIdAsync(sprintIds[0]);
            if (sprint is null)
                throw new InvalidOperationException($"Sprint with Id {sprintIds[0]} does not exist.");
            if (HasEnded(sprint))
                throw new InvalidOperationException($"Cannot add work items to sprint '{sprint.Name}' because it has already ended.");

            await _workItemRepository.AddRangeAsync(items);
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

            if (HasEnded(sprint))
            {
                throw new InvalidOperationException(
                    $"Cannot move a work item into sprint '{sprint.Name}' because it has already ended.");
            }

            _workItemRepository.Update(workItem);
            await _workItemRepository.SaveChangesAsync();
        }

        // Sprint dates are calendar days in the configured business time zone; an end date is inclusive.
        private bool HasEnded(Sprint sprint) =>
            sprint.EndDate.Date < TimeZoneInfo.ConvertTime(_timeProvider.GetUtcNow(), _businessTimeZone).Date;

        public async Task DeleteAsync(int id)
        {
            var workItem = await _workItemRepository.GetByIdAsync(id);
            if (workItem == null) return;

            _workItemRepository.Delete(workItem);
            await _workItemRepository.SaveChangesAsync();
        }
    }
}
