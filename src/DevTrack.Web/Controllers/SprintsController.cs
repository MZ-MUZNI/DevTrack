using DevTrack.Core.Entities;
using DevTrack.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DevTrack.Web.Controllers;

public class SprintsController : Controller
{
    private readonly ISprintRepository _sprintRepository;
    private readonly IRepository<ProjectEntity> _projectRepository;

    // Notice: TWO repositories injected here. Sprints need to know about
    // Projects (for the dropdown), but Sprint and Project are still
    // completely separate repositories - this Controller is just the
    // place that coordinates between them. That coordination role is
    // exactly what will move into a Service layer later.
    public SprintsController(
        ISprintRepository sprintRepository,
        IRepository<ProjectEntity> projectRepository)
    {
        _sprintRepository = sprintRepository;
        _projectRepository = projectRepository;
    }

    // GET: /Sprints
    public async Task<IActionResult> Index()
    {
        // Now using the eager-loaded query, so sprint.Project is populated
        var sprints = await _sprintRepository.GetAllWithProjectAsync();
        return View(sprints);
    }

    // GET: /Sprints/Create
    public async Task<IActionResult> Create()
    {
        await PopulateProjectsDropdown();
        return View();
    }

    // POST: /Sprints/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Name,StartDate,EndDate,ProjectId")] Sprint sprint)
    {
        if (!ModelState.IsValid)
        {
            await PopulateProjectsDropdown(sprint.ProjectId);
            return View(sprint);
        }

        await _sprintRepository.AddAsync(sprint);
        await _sprintRepository.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // GET: /Sprints/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var sprint = await _sprintRepository.GetByIdAsync(id);
        if (sprint is null) return NotFound();

        await PopulateProjectsDropdown(sprint.ProjectId);
        return View(sprint);
    }

    // POST: /Sprints/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Name,StartDate,EndDate,ProjectId")] Sprint sprint)
    {
        if (id != sprint.Id) return BadRequest();

        if (!ModelState.IsValid)
        {
            await PopulateProjectsDropdown(sprint.ProjectId);
            return View(sprint);
        }

        _sprintRepository.Update(sprint);
        await _sprintRepository.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // GET: /Sprints/Delete/5
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(int id)
    {
        var sprint = await _sprintRepository.GetByIdAsync(id);
        if (sprint is null) return NotFound();
        return View(sprint);
    }

    // POST: /Sprints/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var sprint = await _sprintRepository.GetByIdAsync(id);
        if (sprint is null) return NotFound();

        _sprintRepository.Delete(sprint);
        await _sprintRepository.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // Helper: builds the <select> options for ProjectId, and marks the
    // current value as selected (used on Edit, and on Create validation
    // failure so the dropdown doesn't reset to blank).
    private async Task PopulateProjectsDropdown(int? selectedProjectId = null)
    {
        var projects = await _projectRepository.GetAllAsync();
        ViewBag.ProjectId = new SelectList(projects, "Id", "Name", selectedProjectId);
    }
}
