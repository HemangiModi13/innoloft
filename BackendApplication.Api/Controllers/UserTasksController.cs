using BackendApplication.Api.Contracts.Mapping;
using BackendApplication.Api.Contracts.User;
using BackendApplication.Api.Data;
using BackendApplication.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BackendApplication.Api.Controllers;

[ApiController]
[Route("api/users/{userId}")]
public sealed class UserTasksController(AppDbContext dbContext, TaskWorkflowService workflowService) : ControllerBase
{
    [HttpGet("tasks")]
    public async Task<ActionResult<IReadOnlyList<UserTaskSummaryResponse>>> GetAssignedTasks(
        string userId,
        CancellationToken cancellationToken)
    {
        var tasks = await dbContext.Tasks
            .Include(task => task.Steps)
            .Where(task => task.AssignedUserId == userId)
            .OrderBy(task => task.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return Ok(tasks.Select(ResponseMapper.ToUserTaskSummaryResponse).ToList());
    }

    [HttpGet("tasks/{taskId:guid}")]
    public async Task<ActionResult<UserTaskDetailsResponse>> GetAssignedTaskDetails(
        string userId,
        Guid taskId,
        CancellationToken cancellationToken)
    {
        var task = await dbContext.Tasks
            .Include(currentTask => currentTask.Steps)
            .FirstOrDefaultAsync(
                currentTask => currentTask.Id == taskId && currentTask.AssignedUserId == userId,
                cancellationToken);

        if (task is null)
        {
            return NotFound(new { message = "Task not found for user." });
        }

        return Ok(ResponseMapper.ToUserTaskDetailsResponse(task));
    }

    [HttpPost("steps/{stepId:guid}/complete")]
    public async Task<ActionResult<ManualStepCompletionResponse>> CompleteManualStep(
        string userId,
        Guid stepId,
        CancellationToken cancellationToken)
    {
        var result = await workflowService.CompleteManualStepAsync(userId, stepId, cancellationToken);

        if (!result.Succeeded)
        {
            return result.Failure switch
            {
                ManualStepCompletionFailure.StepNotFound => NotFound(new { message = "Step not found." }),
                ManualStepCompletionFailure.UserMismatch => StatusCode(
                    StatusCodes.Status403Forbidden,
                    new { message = "Step does not belong to this user." }),
                ManualStepCompletionFailure.AutomatedStep => BadRequest(new { message = "Step is automated." }),
                ManualStepCompletionFailure.DependencyNotMet => StatusCode(
                    StatusCodes.Status403Forbidden,
                    new { message = "Cannot complete step. Required preceding steps are incomplete." }),
                _ => StatusCode(StatusCodes.Status500InternalServerError, new { message = "Unexpected workflow error." })
            };
        }

        var response = new ManualStepCompletionResponse(
            result.Task!.Id,
            result.Step!.Id,
            result.Task.Status,
            result.Step.Status,
            result.AlreadyDone);

        return Ok(response);
    }
}
