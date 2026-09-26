using DevTrack.Api.Contracts;
using DevTrack.Core.AI;
using DevTrack.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DevTrack.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/ai/task-suggestions")]
public sealed class TaskSuggestionsController(
    ITaskPlanningService taskPlanning,
    ISprintRepository sprints,
    ILogger<TaskSuggestionsController> logger) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<TaskSuggestionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<TaskSuggestionResponse>> Suggest(
        TaskSuggestionRequest request,
        CancellationToken cancellationToken)
    {
        var sprint = await sprints.GetByIdAsync(request.SprintId);
        if (sprint is null)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [nameof(request.SprintId)] = ["The specified sprint does not exist."]
            }));
        }

        try
        {
            var result = await taskPlanning.SuggestSubtasksAsync(
                new TaskPlanningRequest(request.Title.Trim(), request.Description?.Trim(), sprint.StartDate, sprint.EndDate),
                cancellationToken);

            return Ok(new TaskSuggestionResponse(
                result.Summary,
                result.Assumptions,
                result.Subtasks.Select(item => new SuggestedSubtaskResponse(
                    item.Title, item.Description, item.EstimatedHours)).ToList()));
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(exception, "Local task-planning model is unavailable.");
            return Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "The local AI model is unavailable.",
                detail: "Start Ollama and ensure the configured model has been downloaded.");
        }
        catch (InvalidOperationException exception)
        {
            logger.LogWarning(exception, "Local task-planning model returned an invalid response.");
            return Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "The local AI model returned an invalid response.");
        }
    }
}
