using BackendApplication.Api.Data;
using BackendApplication.Api.Domain.Entities;
using BackendApplication.Api.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BackendApplication.Api.Services;

public enum ManualStepCompletionFailure
{
    StepNotFound,
    UserMismatch,
    AutomatedStep,
    DependencyNotMet
}

public sealed record ManualStepCompletionResult(
    bool Succeeded,
    bool AlreadyDone,
    ManualStepCompletionFailure? Failure,
    UserTaskEntity? Task,
    TaskStepEntity? Step);

public enum ModuleEntryProcessingFailure
{
    NoMatchingActiveStep
}

public sealed record ModuleEntryProcessingResult(
    bool Succeeded,
    ModuleEntryProcessingFailure? Failure,
    UserTaskEntity? Task,
    TaskStepEntity? Step);

public sealed class TaskWorkflowService(AppDbContext dbContext)
{
    public async Task<ManualStepCompletionResult> CompleteManualStepAsync(
        string userId,
        Guid stepId,
        CancellationToken cancellationToken = default)
    {
        var step = await dbContext.Steps
            .Include(currentStep => currentStep.Task)
            .ThenInclude(task => task.Steps)
            .FirstOrDefaultAsync(currentStep => currentStep.Id == stepId, cancellationToken);

        if (step is null)
        {
            return new ManualStepCompletionResult(
                false,
                false,
                ManualStepCompletionFailure.StepNotFound,
                null,
                null);
        }

        if (!string.Equals(step.Task.AssignedUserId, userId, StringComparison.Ordinal))
        {
            return new ManualStepCompletionResult(
                false,
                false,
                ManualStepCompletionFailure.UserMismatch,
                step.Task,
                step);
        }

        if (step.Type != StepType.Manual)
        {
            return new ManualStepCompletionResult(
                false,
                false,
                ManualStepCompletionFailure.AutomatedStep,
                step.Task,
                step);
        }

        if (step.Status == StepStatus.DONE)
        {
            return new ManualStepCompletionResult(true, true, null, step.Task, step);
        }

        if (WorkflowEvaluator.IsLocked(step, step.Task.Steps))
        {
            return new ManualStepCompletionResult(
                false,
                false,
                ManualStepCompletionFailure.DependencyNotMet,
                step.Task,
                step);
        }

        step.Status = StepStatus.DONE;
        RefreshTaskStatus(step.Task);

        await dbContext.SaveChangesAsync(cancellationToken);
        return new ManualStepCompletionResult(true, false, null, step.Task, step);
    }

    public async Task<ModuleEntryProcessingResult> ProcessModuleEntryCreatedAsync(
        string userId,
        string moduleKey,
        CancellationToken cancellationToken = default)
    {
        var tasks = await dbContext.Tasks
            .Include(task => task.Steps)
            .Where(task => task.AssignedUserId == userId)
            .ToListAsync(cancellationToken);

        tasks = tasks.OrderBy(task => task.CreatedAtUtc).ToList();

        (TaskStepEntity Step, UserTaskEntity Task)? candidate = null;

        foreach (var task in tasks)
        {
            var orderedSteps = WorkflowEvaluator.SortSteps(task.Steps);
            foreach (var step in orderedSteps)
            {
                if (step.Type != StepType.Automated || step.Status != StepStatus.OPEN)
                {
                    continue;
                }

                if (!string.Equals(step.ModuleKey, moduleKey, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (WorkflowEvaluator.IsLocked(step, orderedSteps))
                {
                    continue;
                }

                candidate = (step, task);
                break;
            }

            if (candidate is not null)
            {
                break;
            }
        }

        if (candidate is null)
        {
            return new ModuleEntryProcessingResult(false, ModuleEntryProcessingFailure.NoMatchingActiveStep, null, null);
        }

        candidate.Value.Step.Status = StepStatus.DONE;
        RefreshTaskStatus(candidate.Value.Task);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new ModuleEntryProcessingResult(true, null, candidate.Value.Task, candidate.Value.Step);
    }

    public void RefreshTaskStatus(UserTaskEntity task)
    {
        if (task.IsStatusForced)
        {
            return;
        }

        task.Status = WorkflowEvaluator.IsTaskDone(task.Steps) ? TaskState.DONE : TaskState.OPEN;
    }
}
