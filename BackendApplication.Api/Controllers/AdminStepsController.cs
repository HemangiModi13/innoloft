using BackendApplication.Api.Contracts.Admin;
using BackendApplication.Api.Contracts.Mapping;
using BackendApplication.Api.Contracts.Shared;
using BackendApplication.Api.Data;
using BackendApplication.Api.Domain.Entities;
using BackendApplication.Api.Domain.Enums;
using BackendApplication.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BackendApplication.Api.Controllers;

[ApiController]
[Route("api/admin")]
public sealed class AdminStepsController(AppDbContext dbContext, TaskWorkflowService workflowService) : ControllerBase
{
    [HttpGet("tasks/{taskId:guid}/steps")]
    public async Task<ActionResult<IReadOnlyList<AdminStepResponse>>> GetStepsForTask(
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

        var steps = WorkflowEvaluator.SortSteps(task.Steps)
            .Select(ResponseMapper.ToAdminStepResponse)
            .ToList();

        return Ok(steps);
    }

    [HttpPost("tasks/{taskId:guid}/steps")]
    public async Task<ActionResult<AdminStepResponse>> CreateStep(
        Guid taskId,
        [FromBody] CreateStepRequest request,
        CancellationToken cancellationToken)
    {
        var task = await dbContext.Tasks
            .Include(currentTask => currentTask.Steps)
            .FirstOrDefaultAsync(currentTask => currentTask.Id == taskId, cancellationToken);

        if (task is null)
        {
            return NotFound(new { message = "Task not found." });
        }

        var validationMessage = ValidateStepRequest(
            request.Title,
            request.Description,
            request.Type,
            request.ModuleKey);

        if (validationMessage is not null)
        {
            return BadRequest(new { message = validationMessage });
        }

        if (task.Steps.Any(step => step.OrderIndex == request.OrderIndex))
        {
            return Conflict(new { message = "OrderIndex must be unique within a task." });
        }

        var step = new TaskStepEntity
        {
            TaskId = task.Id,
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            Icon = NormalizeOptional(request.Icon),
            Type = request.Type,
            IsRequired = request.IsRequired,
            OrderIndex = request.OrderIndex,
            ModuleKey = request.Type == StepType.Automated ? request.ModuleKey!.Trim() : null
        };

        task.Steps.Add(step);
        dbContext.Entry(step).State = EntityState.Added;
        workflowService.RefreshTaskStatus(task);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetStep), new { stepId = step.Id }, ResponseMapper.ToAdminStepResponse(step));
    }

    [HttpGet("steps/{stepId:guid}")]
    public async Task<ActionResult<AdminStepResponse>> GetStep(Guid stepId, CancellationToken cancellationToken)
    {
        var step = await dbContext.Steps
            .FirstOrDefaultAsync(currentStep => currentStep.Id == stepId, cancellationToken);

        if (step is null)
        {
            return NotFound(new { message = "Step not found." });
        }

        return Ok(ResponseMapper.ToAdminStepResponse(step));
    }

    [HttpPut("steps/{stepId:guid}")]
    public async Task<ActionResult<AdminStepResponse>> UpdateStep(
        Guid stepId,
        [FromBody] UpdateStepRequest request,
        CancellationToken cancellationToken)
    {
        var step = await dbContext.Steps
            .Include(currentStep => currentStep.Task)
            .ThenInclude(task => task.Steps)
            .FirstOrDefaultAsync(currentStep => currentStep.Id == stepId, cancellationToken);

        if (step is null)
        {
            return NotFound(new { message = "Step not found." });
        }

        var validationMessage = ValidateStepRequest(
            request.Title,
            request.Description,
            request.Type,
            request.ModuleKey);

        if (validationMessage is not null)
        {
            return BadRequest(new { message = validationMessage });
        }

        if (step.Task.Steps.Any(currentStep => currentStep.Id != step.Id && currentStep.OrderIndex == request.OrderIndex))
        {
            return Conflict(new { message = "OrderIndex must be unique within a task." });
        }

        step.Title = request.Title.Trim();
        step.Description = request.Description.Trim();
        step.Icon = NormalizeOptional(request.Icon);
        step.Type = request.Type;
        step.IsRequired = request.IsRequired;
        step.OrderIndex = request.OrderIndex;
        step.ModuleKey = request.Type == StepType.Automated ? request.ModuleKey!.Trim() : null;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(ResponseMapper.ToAdminStepResponse(step));
    }

    [HttpPost("steps/{stepId:guid}/force-status")]
    public async Task<ActionResult<AdminStepResponse>> ForceStepStatus(
        Guid stepId,
        [FromBody] ForceStepStatusRequest request,
        CancellationToken cancellationToken)
    {
        var step = await dbContext.Steps
            .Include(currentStep => currentStep.Task)
            .ThenInclude(task => task.Steps)
            .FirstOrDefaultAsync(currentStep => currentStep.Id == stepId, cancellationToken);

        if (step is null)
        {
            return NotFound(new { message = "Step not found." });
        }

        step.Status = request.Status;
        workflowService.RefreshTaskStatus(step.Task);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(ResponseMapper.ToAdminStepResponse(step));
    }

    [HttpDelete("steps/{stepId:guid}")]
    public async Task<IActionResult> DeleteStep(Guid stepId, CancellationToken cancellationToken)
    {
        var step = await dbContext.Steps
            .Include(currentStep => currentStep.Task)
            .ThenInclude(task => task.Steps)
            .FirstOrDefaultAsync(currentStep => currentStep.Id == stepId, cancellationToken);

        if (step is null)
        {
            return NotFound(new { message = "Step not found." });
        }

        dbContext.Steps.Remove(step);
        workflowService.RefreshTaskStatus(step.Task);
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private static string? ValidateStepRequest(string title, string description, StepType type, string? moduleKey)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return "Title is required.";
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            return "Description is required.";
        }

        if (type == StepType.Automated && string.IsNullOrWhiteSpace(moduleKey))
        {
            return "ModuleKey is required for automated steps.";
        }

        if (type == StepType.Manual && !string.IsNullOrWhiteSpace(moduleKey))
        {
            return "ModuleKey must be empty for manual steps.";
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
