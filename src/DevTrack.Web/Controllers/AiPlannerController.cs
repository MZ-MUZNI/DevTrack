using DevTrack.Core.AI;
using DevTrack.Application.Interfaces;
using DevTrack.Core.Entities;
using DevTrack.Core.Interfaces;
using DevTrack.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DevTrack.Web.Controllers;

public sealed class AiPlannerController(
    ITaskPlanningService taskPlanning,
    ISprintRepository sprints,
    IWorkItemService workItems,
    ILogger<AiPlannerController> logger) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var model = new AiPlannerViewModel();
        await PopulateSprintsAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Generate(AiPlannerViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await PopulateSprintsAsync(model);
            return View(nameof(Index), model);
        }

        var sprint = await sprints.GetByIdAsync(model.SprintId);
        if (sprint is null)
        {
            ModelState.AddModelError(nameof(model.SprintId), "The selected sprint no longer exists.");
            await PopulateSprintsAsync(model);
            return View(nameof(Index), model);
        }

        try
        {
            var plan = await taskPlanning.SuggestSubtasksAsync(
                new TaskPlanningRequest(model.Title.Trim(), model.Description?.Trim(), sprint.StartDate, sprint.EndDate),
                cancellationToken);

            model.Summary = plan.Summary;
            model.Assumptions = plan.Assumptions;
            model.Suggestions = plan.Subtasks.Select(item => new AiSuggestedWorkItemViewModel
            {
                Title = item.Title,
                Description = item.Description,
                EstimatedHours = item.EstimatedHours
            }).ToList();
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(exception, "Local AI model is unavailable.");
            ModelState.AddModelError(string.Empty, "The local AI model is unavailable. Start Ollama and confirm Gemma 4 is installed.");
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(exception, "Local AI model did not respond before the timeout.");
            ModelState.AddModelError(string.Empty, "The local AI model took longer than three minutes to respond. Try again after it has warmed up, or use a smaller model.");
        }
        catch (InvalidOperationException exception)
        {
            logger.LogWarning(exception, "Local AI model returned an invalid plan.");
            ModelState.AddModelError(string.Empty, "The local AI model could not produce a usable plan. Please try again.");
        }

        await PopulateSprintsAsync(model);
        return View(nameof(Index), model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSelected(AiPlannerViewModel model)
    {
        if (!model.Suggestions.Any(suggestion => suggestion.Selected))
        {
            ModelState.AddModelError(string.Empty, "Select at least one suggestion to create work items.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateSprintsAsync(model);
            return View(nameof(Index), model);
        }

        try
        {
            foreach (var suggestion in model.Suggestions.Where(suggestion => suggestion.Selected))
            {
                await workItems.CreateAsync(new WorkItem
                {
                    Title = suggestion.Title.Trim(),
                    Description = suggestion.Description.Trim(),
                    EstimatedHours = suggestion.EstimatedHours,
                    SprintId = model.SprintId,
                    Status = WorkItemStatus.Backlog
                });
            }
        }
        catch (InvalidOperationException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            await PopulateSprintsAsync(model);
            return View(nameof(Index), model);
        }

        TempData["Success"] = "Selected AI suggestions were created as backlog work items.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateSprintsAsync(AiPlannerViewModel model)
    {
        model.Sprints = (await sprints.GetAllAsync())
            .Select(sprint => new SelectListItem(sprint.Name, sprint.Id.ToString(), sprint.Id == model.SprintId))
            .ToList();
    }
}
