using DevTrack.Api.Contracts;
using DevTrack.Core.Entities;
using DevTrack.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DevTrack.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/sprints")]
public sealed class SprintsController(
    ISprintRepository sprints,
    IRepository<ProjectEntity> projects,
    IAuthorizationService authorization) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<SprintResponse>>> GetAll() =>
        Ok((await sprints.GetAllWithProjectAsync()).Where(sprint => CanAccess(sprint.Project)).Select(ToResponse));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SprintResponse>> GetById(int id)
    {
        var sprint = await sprints.GetByIdAsync(id);
        if (sprint is null || !await CanAccessProjectAsync(sprint.ProjectId)) return NotFound();
        return Ok(ToResponse(sprint));
    }

    [HttpPost]
    public async Task<ActionResult<SprintResponse>> Create(SprintRequest request)
    {
        if (!await CanAccessProjectAsync(request.ProjectId))
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [nameof(request.ProjectId)] = ["The specified project does not exist."]
            }));
        }

        var sprint = ToEntity(request);
        await sprints.AddAsync(sprint);
        await sprints.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { sprint.Id }, ToResponse(sprint));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<SprintResponse>> Update(int id, SprintRequest request)
    {
        var sprint = await sprints.GetByIdAsync(id);
        if (sprint is null || !await CanAccessProjectAsync(sprint.ProjectId)) return NotFound();
        if (!await CanAccessProjectAsync(request.ProjectId))
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [nameof(request.ProjectId)] = ["The specified project does not exist."]
            }));
        }

        sprint.Name = request.Name.Trim();
        sprint.StartDate = request.StartDate;
        sprint.EndDate = request.EndDate;
        sprint.ProjectId = request.ProjectId;
        sprints.Update(sprint);
        await sprints.SaveChangesAsync();
        return Ok(ToResponse(sprint));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(int id)
    {
        var sprint = await sprints.GetByIdAsync(id);
        if (sprint is null || !await CanAccessProjectAsync(sprint.ProjectId)) return NotFound();
        sprints.Delete(sprint);
        await sprints.SaveChangesAsync();
        return NoContent();
    }

    private static Sprint ToEntity(SprintRequest request) => new()
    {
        Name = request.Name.Trim(), StartDate = request.StartDate,
        EndDate = request.EndDate, ProjectId = request.ProjectId
    };

    private static SprintResponse ToResponse(Sprint sprint) => new(
        sprint.Id, sprint.Name, sprint.StartDate, sprint.EndDate,
        sprint.ProjectId, sprint.Project?.Name);

    private bool CanAccess(ProjectEntity? project) => project is not null &&
        (User.IsInRole("Admin") || project.OwnerId == User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);

    private async Task<bool> CanAccessProjectAsync(int projectId)
    {
        var project = await projects.GetByIdAsync(projectId);
        return project is not null && (await authorization.AuthorizeAsync(User, project, "ProjectAccess")).Succeeded;
    }
}
