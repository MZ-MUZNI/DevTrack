using DevTrack.Api.Contracts;
using DevTrack.Core.Entities;
using DevTrack.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DevTrack.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/projects")]
public sealed class ProjectsController(IRepository<ProjectEntity> projects) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IEnumerable<ProjectResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ProjectResponse>>> GetAll()
    {
        var results = (await projects.GetAllAsync()).Select(ToResponse);
        return Ok(results);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<ProjectResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProjectResponse>> GetById(int id)
    {
        var project = await projects.GetByIdAsync(id);
        return project is null ? NotFound() : Ok(ToResponse(project));
    }

    [HttpPost]
    [ProducesResponseType<ProjectResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<ProjectResponse>> Create(ProjectRequest request)
    {
        var project = new ProjectEntity { Name = request.Name.Trim(), Description = request.Description?.Trim() };
        await projects.AddAsync(project);
        await projects.SaveChangesAsync();
        var response = ToResponse(project);
        return CreatedAtAction(nameof(GetById), new { project.Id }, response);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType<ProjectResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProjectResponse>> Update(int id, ProjectRequest request)
    {
        var project = await projects.GetByIdAsync(id);
        if (project is null) return NotFound();

        project.Name = request.Name.Trim();
        project.Description = request.Description?.Trim();
        projects.Update(project);
        await projects.SaveChangesAsync();
        return Ok(ToResponse(project));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var project = await projects.GetByIdAsync(id);
        if (project is null) return NotFound();

        projects.Delete(project);
        await projects.SaveChangesAsync();
        return NoContent();
    }

    private static ProjectResponse ToResponse(ProjectEntity project) =>
        new(project.Id, project.Name, project.Description, project.CreatedAt);
}
