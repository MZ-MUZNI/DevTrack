using DevTrack.Core.Entities;
using DevTrack.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Threading.Tasks;
using DevTrack.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using DevTrack.Web.Models;
using System.Security.Claims;

namespace DevTrack.Web.Controllers
{
    public class WorkItemsController : Controller
    {
        private readonly IWorkItemService _workItemService;
        private readonly ISprintRepository _sprintRepository;
        private readonly IRepository<ProjectEntity> _projectRepository;

        public WorkItemsController(
            IWorkItemService workItemService,
            ISprintRepository sprintRepository,
            IRepository<ProjectEntity> projectRepository)
        {
            _workItemService = workItemService;
            _sprintRepository = sprintRepository;
            _projectRepository = projectRepository;
        }

        // GET: WorkItems
        public async Task<IActionResult> Index()
        {
            var workItems = (await _workItemService.GetAllAsync()).Where(item => CanAccess(item.Sprint?.Project));
            return View(workItems);
        }

        // GET: WorkItems/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var workItem = await _workItemService.GetByIdAsync(id);
            if (workItem == null || !await CanAccessSprintAsync(workItem.SprintId)) return NotFound();
            return View(workItem);
        }

        // GET: WorkItems/Create
        public async Task<IActionResult> Create()
        {
            ViewBag.SprintId = new SelectList(
                (await _sprintRepository.GetAllWithProjectAsync()).Where(sprint => CanAccess(sprint.Project)), "Id", "Name");
            return View();
        }

        // POST: WorkItems/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(WorkItemFormViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (!await CanAccessSprintAsync(model.SprintId)) return NotFound();
                    var workItem = ToEntity(model);
                    await _workItemService.CreateAsync(workItem);
                    return RedirectToAction(nameof(Index));
                }
                catch (InvalidOperationException ex)
                {
                    ModelState.AddModelError(string.Empty, ex.Message);
                }
            }

            // Reached if validation failed OR the service threw a business-rule error.
            // Either way, the dropdown needs repopulating before re-rendering the form.
            ViewBag.SprintId = new SelectList(
                (await _sprintRepository.GetAllWithProjectAsync()).Where(sprint => CanAccess(sprint.Project)), "Id", "Name");
            return View(model);
        }

        // GET: WorkItems/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var workItem = await _workItemService.GetByIdAsync(id);
            if (workItem == null || !await CanAccessSprintAsync(workItem.SprintId)) return NotFound();

            ViewBag.SprintId = new SelectList(
                (await _sprintRepository.GetAllWithProjectAsync()).Where(sprint => CanAccess(sprint.Project)), "Id", "Name", workItem.SprintId);
            return View(ToFormModel(workItem));
        }

        // POST: WorkItems/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, WorkItemFormViewModel model)
        {
            var existingWorkItem = await _workItemService.GetByIdAsync(id);
            if (id != model.Id || existingWorkItem is null ||
                !await CanAccessSprintAsync(existingWorkItem.SprintId) ||
                !await CanAccessSprintAsync(model.SprintId)) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var workItem = ToEntity(model);
                    await _workItemService.UpdateAsync(workItem);
                    return RedirectToAction(nameof(Index));
                }
                catch (InvalidOperationException ex)
                {
                    ModelState.AddModelError(string.Empty, ex.Message);
                }

            }

            ViewBag.SprintId = new SelectList(
                (await _sprintRepository.GetAllWithProjectAsync()).Where(sprint => CanAccess(sprint.Project)), "Id", "Name", model.SprintId);
            return View(model);
        }

        // GET: WorkItems/Delete/5
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Delete(int id)
        {
            var workItem = await _workItemService.GetByIdAsync(id);
            if (workItem == null || !await CanAccessSprintAsync(workItem.SprintId)) return NotFound();
            return View(workItem);
        }

        // POST: WorkItems/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _workItemService.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }

        private static WorkItem ToEntity(WorkItemFormViewModel model) => new()
        {
            Id = model.Id, Title = model.Title.Trim(), Description = model.Description?.Trim(),
            Status = model.Status, AssignedTo = model.AssignedTo?.Trim(),
            EstimatedHours = model.EstimatedHours, SprintId = model.SprintId
        };

        private static WorkItemFormViewModel ToFormModel(WorkItem workItem) => new()
        {
            Id = workItem.Id, Title = workItem.Title, Description = workItem.Description,
            Status = workItem.Status, AssignedTo = workItem.AssignedTo,
            EstimatedHours = workItem.EstimatedHours, SprintId = workItem.SprintId
        };

        private bool CanAccess(ProjectEntity? project) => project is not null &&
            (User.IsInRole("Admin") || project.OwnerId == User.FindFirstValue(ClaimTypes.NameIdentifier));

        private async Task<bool> CanAccessSprintAsync(int sprintId)
        {
            var sprint = await _sprintRepository.GetByIdAsync(sprintId);
            if (sprint is null) return false;
            return CanAccess(await _projectRepository.GetByIdAsync(sprint.ProjectId));
        }
    }
}
