using BackendApplication.Api.Contracts.Admin;
using BackendApplication.Api.Contracts.Mapping;
using BackendApplication.Api.Contracts.Shared;
using BackendApplication.Api.Data;
using BackendApplication.Api.Domain.Entities;
using BackendApplication.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BackendApplication.Api.Controllers;

[ApiController]
[Route("api/admin/tasks")]
public sealed class AdminTasksController(AppDbContext dbContext, TaskWorkflowService workflowService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AdminTaskResponse>>> GetTasks(CancellationToken cancellationToken)
    {
        var tasks = await dbContext.Tasks
            .Include(task => task.Steps)
            .OrderBy(task => task.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return Ok(tasks.Select(ResponseMapper.ToAdminTaskResponse).ToList());
    }

    [HttpGet("{taskId:guid}")]
    public async Task<ActionResult<AdminTaskResponse>> GetTask(Guid taskId, CancellationToken cancellationToken)
    {
        var task = await dbContext.Tasks
            .Include(currentTask => currentTask.Steps)
            .FirstOrDefaultAsync(currentTask => currentTask.Id == taskId, cancellationToken);

        if (task is null)
        {
            return NotFound(new { message = "Task not found." });
        }

        return Ok(ResponseMapper.ToAdminTaskResponse(task));
    }

    [HttpPost]
    public async Task<ActionResult<AdminTaskResponse>> CreateTask(
        [FromBody] CreateTaskRequest request,
        CancellationToken cancellationToken)
    {
        var validationMessage = ValidateTaskPayload(request.Title, request.Description, request.AssignedUserId);
        if (validationMessage is not null)
        {
            return BadRequest(new { message = validationMessage });
        }

        var task = new UserTaskEntity
        {
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            Icon = NormalizeOptional(request.Icon),
            AssignedUserId = request.AssignedUserId.Trim()
        };

        dbContext.Tasks.Add(task);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetTask), new { taskId = task.Id }, ResponseMapper.ToAdminTaskResponse(task));
    }

    [HttpPut("{taskId:guid}")]
    public async Task<ActionResult<AdminTaskResponse>> UpdateTask(
        Guid taskId,
        [FromBody] UpdateTaskRequest request,
        CancellationToken cancellationToken)
    {
        var task = await dbContext.Tasks
            .Include(currentTask => currentTask.Steps)
            .FirstOrDefaultAsync(currentTask => currentTask.Id == taskId, cancellationToken);

        if (task is null)
        {
            return NotFound(new { message = "Task not found." });
        }

        var validationMessage = ValidateTaskPayload(request.Title, request.Description, request.AssignedUserId);
        if (validationMessage is not null)
        {
            return BadRequest(new { message = validationMessage });
        }

        task.Title = request.Title.Trim();
        task.Description = request.Description.Trim();
        task.Icon = NormalizeOptional(request.Icon);
        task.AssignedUserId = request.AssignedUserId.Trim();

        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(ResponseMapper.ToAdminTaskResponse(task));
    }

    [HttpPost("{taskId:guid}/assign")]
    public async Task<ActionResult<AdminTaskResponse>> AssignTask(
        Guid taskId,
        [FromBody] AssignTaskRequest request,
        CancellationToken cancellationToken)
    {
        var task = await dbContext.Tasks
            .Include(currentTask => currentTask.Steps)
            .FirstOrDefaultAsync(currentTask => currentTask.Id == taskId, cancellationToken);

        if (task is null)
        {
            return NotFound(new { message = "Task not found." });
        }

        if (string.IsNullOrWhiteSpace(request.AssignedUserId))
        {
            return BadRequest(new { message = "AssignedUserId is required." });
        }

        task.AssignedUserId = request.AssignedUserId.Trim();
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(ResponseMapper.ToAdminTaskResponse(task));
    }

    [HttpPost("{taskId:guid}/force-status")]
    public async Task<ActionResult<AdminTaskResponse>> ForceTaskStatus(
        Guid taskId,
        [FromBody] ForceTaskStatusRequest request,
        CancellationToken cancellationToken)
    {
        var task = await dbContext.Tasks
            .Include(currentTask => currentTask.Steps)
            .FirstOrDefaultAsync(currentTask => currentTask.Id == taskId, cancellationToken);

        if (task is null)
        {
            return NotFound(new { message = "Task not found." });
        }

        task.Status = request.Status;
        task.IsStatusForced = true;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(ResponseMapper.ToAdminTaskResponse(task));
    }

    [HttpPost("{taskId:guid}/status/reset")]
    public async Task<ActionResult<AdminTaskResponse>> ResetTaskStatusAutomation(
        Guid taskId,
        CancellationToken cancellationToken)
    {
        var task = await dbContext.Tasks
            .Include(currentTask => currentTask.Steps)
            .FirstOrDefaultAsync(currentTask => currentTask.Id == taskId, cancellationToken);

        if (task is null)
        {
            return NotFound(new { message = "Task not found." });
        }

        task.IsStatusForced = false;
        workflowService.RefreshTaskStatus(task);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(ResponseMapper.ToAdminTaskResponse(task));
    }

    [HttpDelete("{taskId:guid}")]
    public async Task<IActionResult> DeleteTask(Guid taskId, CancellationToken cancellationToken)
    {
        var task = await dbContext.Tasks
            .FirstOrDefaultAsync(currentTask => currentTask.Id == taskId, cancellationToken);

        if (task is null)
        {
            return NotFound(new { message = "Task not found." });
        }

        dbContext.Tasks.Remove(task);
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private static string? ValidateTaskPayload(string title, string description, string assignedUserId)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return "Title is required.";
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            return "Description is required.";
        }

        if (string.IsNullOrWhiteSpace(assignedUserId))
        {
            return "AssignedUserId is required.";
        }

        return null;
    }

    private static string? NormalizeOptional(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }
}
