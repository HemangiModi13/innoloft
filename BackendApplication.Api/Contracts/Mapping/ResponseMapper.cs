using BackendApplication.Api.Contracts.Shared;
using BackendApplication.Api.Contracts.User;
using BackendApplication.Api.Domain.Entities;
using BackendApplication.Api.Domain.Enums;
using BackendApplication.Api.Services;

namespace BackendApplication.Api.Contracts.Mapping;

public static class ResponseMapper
{
    public static AdminStepResponse ToAdminStepResponse(TaskStepEntity step)
    {
        return new AdminStepResponse(
            step.Id,
            step.Title,
            step.Description,
            step.Icon,
            step.Status,
            step.Type,
            step.IsRequired,
            step.OrderIndex,
            step.ModuleKey);
    }

    public static AdminTaskResponse ToAdminTaskResponse(UserTaskEntity task)
    {
        var orderedSteps = WorkflowEvaluator.SortSteps(task.Steps);
        var mappedSteps = orderedSteps.Select(ToAdminStepResponse).ToList();
        var completedSteps = mappedSteps.Count(step => step.Status == StepStatus.DONE);

        return new AdminTaskResponse(
            task.Id,
            task.Title,
            task.Description,
            task.Icon,
            task.AssignedUserId,
            task.Status,
            task.IsStatusForced,
            mappedSteps.Count,
            completedSteps,
            mappedSteps);
    }

    public static UserTaskSummaryResponse ToUserTaskSummaryResponse(UserTaskEntity task)
    {
        var orderedSteps = WorkflowEvaluator.SortSteps(task.Steps);
        var completedSteps = orderedSteps.Count(step => step.Status == StepStatus.DONE);
        var remainingSteps = orderedSteps.Count(step => step.Status == StepStatus.OPEN);
        var lockedSteps = orderedSteps.Count(step => WorkflowEvaluator.IsLocked(step, orderedSteps));
        var nextStep = orderedSteps.FirstOrDefault(step =>
            step.Status == StepStatus.OPEN && !WorkflowEvaluator.IsLocked(step, orderedSteps));

        return new UserTaskSummaryResponse(
            task.Id,
            task.Title,
            task.Description,
            task.Icon,
            task.Status,
            orderedSteps.Count,
            completedSteps,
            remainingSteps,
            lockedSteps,
            nextStep?.Id,
            nextStep?.Title);
    }

    public static UserTaskDetailsResponse ToUserTaskDetailsResponse(UserTaskEntity task)
    {
        var orderedSteps = WorkflowEvaluator.SortSteps(task.Steps);
        var mappedSteps = orderedSteps
            .Select(step =>
            {
                var isLocked = WorkflowEvaluator.IsLocked(step, orderedSteps);
                var canCompleteManually = step.Type == StepType.Manual
                    && step.Status == StepStatus.OPEN
                    && !isLocked;

                return new UserStepResponse(
                    step.Id,
                    step.Title,
                    step.Description,
                    step.Icon,
                    step.Status,
                    step.Type,
                    step.IsRequired,
                    step.OrderIndex,
                    step.ModuleKey,
                    isLocked,
                    canCompleteManually);
            })
            .ToList();

        return new UserTaskDetailsResponse(
            task.Id,
            task.Title,
            task.Description,
            task.Icon,
            task.Status,
            mappedSteps.Count,
            mappedSteps.Count(step => step.Status == StepStatus.DONE),
            WorkflowEvaluator.GetCurrentStepNumber(orderedSteps),
            mappedSteps);
    }
}
