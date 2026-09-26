using DevTrack.Application.Services;
using DevTrack.Core.Entities;
using DevTrack.Core.Interfaces;

namespace DevTrack.Tests;

public sealed class WorkItemServiceTests
{
    [Fact]
    public async Task CreateAsync_WhenSprintIsActive_AddsAndSavesWorkItem()
    {
        var workItems = new FakeWorkItemRepository();
        var service = CreateService(workItems, new Sprint { Id = 7, Name = "Current sprint", EndDate = DateTime.Now.AddDays(1) });
        var workItem = new WorkItem { SprintId = 7, Title = "Write tests" };

        await service.CreateAsync(workItem);

        Assert.Same(workItem, Assert.Single(workItems.Added));
        Assert.Equal(1, workItems.SaveChangesCalls);
    }

    [Fact]
    public async Task CreateAsync_WhenSprintDoesNotExist_ThrowsAndDoesNotPersist()
    {
        var workItems = new FakeWorkItemRepository();
        var service = CreateService(workItems, null);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(new WorkItem { SprintId = 42 }));

        Assert.Contains("Sprint with Id 42 does not exist.", exception.Message);
        Assert.Empty(workItems.Added);
        Assert.Equal(0, workItems.SaveChangesCalls);
    }

    [Fact]
    public async Task CreateAsync_WhenSprintHasEnded_ThrowsAndDoesNotPersist()
    {
        var workItems = new FakeWorkItemRepository();
        var service = CreateService(workItems, new Sprint { Id = 7, Name = "Closed sprint", EndDate = DateTime.Now.AddDays(-1) });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(new WorkItem { SprintId = 7 }));

        Assert.Contains("Closed sprint", exception.Message);
        Assert.Empty(workItems.Added);
        Assert.Equal(0, workItems.SaveChangesCalls);
    }

    [Fact]
    public async Task UpdateAsync_WhenSprintIsActive_UpdatesAndSavesWorkItem()
    {
        var workItems = new FakeWorkItemRepository();
        var service = CreateService(workItems, new Sprint { Id = 7, EndDate = DateTime.Now.AddDays(1) });
        var workItem = new WorkItem { Id = 3, SprintId = 7 };

        await service.UpdateAsync(workItem);

        Assert.Same(workItem, Assert.Single(workItems.Updated));
        Assert.Equal(1, workItems.SaveChangesCalls);
    }

    [Fact]
    public async Task UpdateAsync_WhenSprintHasEnded_ThrowsAndDoesNotPersist()
    {
        var workItems = new FakeWorkItemRepository();
        var service = CreateService(workItems, new Sprint { Id = 7, Name = "Closed sprint", EndDate = DateTime.Now.AddDays(-1) });

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateAsync(new WorkItem { SprintId = 7 }));

        Assert.Empty(workItems.Updated);
        Assert.Equal(0, workItems.SaveChangesCalls);
    }

    [Fact]
    public async Task UpdateAsync_WhenSprintDoesNotExist_ThrowsAndDoesNotPersist()
    {
        var workItems = new FakeWorkItemRepository();
        var service = CreateService(workItems, null);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateAsync(new WorkItem { SprintId = 42 }));

        Assert.Contains("Sprint with Id 42 does not exist.", exception.Message);
        Assert.Empty(workItems.Updated);
        Assert.Equal(0, workItems.SaveChangesCalls);
    }

    [Fact]
    public async Task DeleteAsync_WhenWorkItemExists_DeletesAndSaves()
    {
        var workItem = new WorkItem { Id = 3 };
        var workItems = new FakeWorkItemRepository { WorkItemById = workItem };
        var service = CreateService(workItems, null);

        await service.DeleteAsync(3);

        Assert.Same(workItem, Assert.Single(workItems.Deleted));
        Assert.Equal(1, workItems.SaveChangesCalls);
    }

    [Fact]
    public async Task DeleteAsync_WhenWorkItemDoesNotExist_DoesNotPersist()
    {
        var workItems = new FakeWorkItemRepository();
        var service = CreateService(workItems, null);

        await service.DeleteAsync(3);

        Assert.Empty(workItems.Deleted);
        Assert.Equal(0, workItems.SaveChangesCalls);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsRepositoryResults()
    {
        var expected = new[] { new WorkItem { Id = 1 }, new WorkItem { Id = 2 } };
        var workItems = new FakeWorkItemRepository { WorkItemsWithSprint = expected };
        var service = CreateService(workItems, null);

        var result = await service.GetAllAsync();

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsRepositoryResult()
    {
        var expected = new WorkItem { Id = 3 };
        var workItems = new FakeWorkItemRepository { WorkItemById = expected };
        var service = CreateService(workItems, null);

        var result = await service.GetByIdAsync(3);

        Assert.Same(expected, result);
    }

    private static WorkItemService CreateService(FakeWorkItemRepository workItems, Sprint? sprint) =>
        new(workItems, new FakeSprintRepository { SprintById = sprint });

    private sealed class FakeWorkItemRepository : IWorkItemRepository
    {
        public List<WorkItem> Added { get; } = [];
        public List<WorkItem> Updated { get; } = [];
        public List<WorkItem> Deleted { get; } = [];
        public int SaveChangesCalls { get; private set; }
        public WorkItem? WorkItemById { get; init; }
        public IEnumerable<WorkItem> WorkItemsWithSprint { get; init; } = [];

        public Task<WorkItem?> GetByIdAsync(int id) => Task.FromResult(WorkItemById);
        public Task<IEnumerable<WorkItem>> GetAllAsync() => Task.FromResult<IEnumerable<WorkItem>>([]);
        public Task AddAsync(WorkItem entity) { Added.Add(entity); return Task.CompletedTask; }
        public void Update(WorkItem entity) => Updated.Add(entity);
        public void Delete(WorkItem entity) => Deleted.Add(entity);
        public Task<int> SaveChangesAsync() { SaveChangesCalls++; return Task.FromResult(1); }
        public Task<IEnumerable<WorkItem>> GetAllWithSprintAsync() => Task.FromResult(WorkItemsWithSprint);
        public Task<WorkItem?> GetByIdWithSprintAsync(int id) => Task.FromResult(WorkItemById);
    }

    private sealed class FakeSprintRepository : ISprintRepository
    {
        public Sprint? SprintById { get; init; }

        public Task<Sprint?> GetByIdAsync(int id) => Task.FromResult(SprintById);
        public Task<IEnumerable<Sprint>> GetAllAsync() => Task.FromResult<IEnumerable<Sprint>>([]);
        public Task AddAsync(Sprint entity) => Task.CompletedTask;
        public void Update(Sprint entity) { }
        public void Delete(Sprint entity) { }
        public Task<int> SaveChangesAsync() => Task.FromResult(1);
        public Task<IEnumerable<Sprint>> GetAllWithProjectAsync() => Task.FromResult<IEnumerable<Sprint>>([]);
    }
}
