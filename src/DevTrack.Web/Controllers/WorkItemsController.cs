using DevTrack.Core.Entities;
using DevTrack.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Threading.Tasks;
using DevTrack.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace DevTrack.Web.Controllers
{
    public class WorkItemsController : Controller
    {
        private readonly IWorkItemService _workItemService;
        private readonly ISprintRepository _sprintRepository;

        public WorkItemsController(
            IWorkItemService workItemService,
            ISprintRepository sprintRepository)
        {
            _workItemService = workItemService;
            _sprintRepository = sprintRepository;
        }

        // GET: WorkItems
        public async Task<IActionResult> Index()
        {
            var workItems = await _workItemService.GetAllAsync();
            return View(workItems);
        }

        // GET: WorkItems/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var workItem = await _workItemService.GetByIdAsync(id);
            if (workItem == null) return NotFound();
            return View(workItem);
        }

        // GET: WorkItems/Create
        public async Task<IActionResult> Create()
        {
            ViewBag.SprintId = new SelectList(
                await _sprintRepository.GetAllAsync(), "Id", "Name");
            return View();
        }

        // POST: WorkItems/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(WorkItem workItem)
        {
            if (ModelState.IsValid)
            {
                try
                {
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
                await _sprintRepository.GetAllAsync(), "Id", "Name");
            return View(workItem);
        }

        // GET: WorkItems/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var workItem = await _workItemService.GetByIdAsync(id);
            if (workItem == null) return NotFound();

            ViewBag.SprintId = new SelectList(
                await _sprintRepository.GetAllAsync(), "Id", "Name", workItem.SprintId);
            return View(workItem);
        }

        // POST: WorkItems/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, WorkItem workItem)
        {
            if (id != workItem.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    await _workItemService.UpdateAsync(workItem);
                    return RedirectToAction(nameof(Index));
                }
                catch (InvalidOperationException ex)
                {
                    ModelState.AddModelError(string.Empty, ex.Message);
                }

            }

            ViewBag.SprintId = new SelectList(
                await _sprintRepository.GetAllAsync(), "Id", "Name", workItem.SprintId);
            return View(workItem);
        }

        // GET: WorkItems/Delete/5
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Delete(int id)
        {
            var workItem = await _workItemService.GetByIdAsync(id);
            if (workItem == null) return NotFound();
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
    }
}
