using DevTrack.Api.Contracts;
using DevTrack.Application.Interfaces;
using DevTrack.Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DevTrack.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/work-items")]
public sealed class WorkItemsController(IWorkItemService workItems) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<WorkItemResponse>>> GetAll() =>
        Ok((await workItems.GetAllAsync()).Select(ToResponse));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<WorkItemResponse>> GetById(int id)
    {
        var workItem = await workItems.GetByIdAsync(id);
        return workItem is null ? NotFound() : Ok(ToResponse(workItem));
    }

    [HttpPost]
    public async Task<ActionResult<WorkItemResponse>> Create(WorkItemRequest request)
    {
        var workItem = ToEntity(request);
        var validation = await TryCreate(workItem);
        if (validation is not null) return validation;

        return CreatedAtAction(nameof(GetById), new { workItem.Id }, ToResponse(workItem));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<WorkItemResponse>> Update(int id, WorkItemRequest request)
    {
        var workItem = await workItems.GetByIdAsync(id);
        if (workItem is null) return NotFound();

        Apply(request, workItem);
        try
        {
            await workItems.UpdateAsync(workItem);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new ValidationProblemDetails(
                new Dictionary<string, string[]> { [nameof(request.SprintId)] = [exception.Message] }));
        }

        return Ok(ToResponse(workItem));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(int id)
    {
        if (await workItems.GetByIdAsync(id) is null) return NotFound();
        await workItems.DeleteAsync(id);
        return NoContent();
    }

    private async Task<ActionResult?> TryCreate(WorkItem workItem)
    {
        try
        {
            await workItems.CreateAsync(workItem);
            return null;
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new ValidationProblemDetails(
                new Dictionary<string, string[]> { ["sprintId"] = [exception.Message] }));
        }
    }

    private static WorkItem ToEntity(WorkItemRequest request)
    {
        var workItem = new WorkItem();
        Apply(request, workItem);
        return workItem;
    }

    private static void Apply(WorkItemRequest request, WorkItem workItem)
    {
        workItem.Title = request.Title.Trim();
        workItem.Description = request.Description?.Trim();
        workItem.Status = request.Status;
        workItem.AssignedTo = request.AssignedTo?.Trim();
        workItem.EstimatedHours = request.EstimatedHours;
        workItem.SprintId = request.SprintId;
    }

    private static WorkItemResponse ToResponse(WorkItem workItem) => new(
        workItem.Id, workItem.Title, workItem.Description, workItem.Status,
        workItem.AssignedTo, workItem.EstimatedHours, workItem.SprintId,
        workItem.Sprint?.Name);
}
