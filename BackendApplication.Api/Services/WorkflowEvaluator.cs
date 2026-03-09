using BackendApplication.Api.Domain.Entities;
using BackendApplication.Api.Domain.Enums;

namespace BackendApplication.Api.Services;

public static class WorkflowEvaluator
{
    public static IReadOnlyList<TaskStepEntity> SortSteps(IEnumerable<TaskStepEntity> steps)
    {
        return steps
            .OrderBy(step => step.OrderIndex)
            .ThenBy(step => step.CreatedAtUtc)
            .ThenBy(step => step.Id)
            .ToList();
    }

    public static bool IsLocked(TaskStepEntity step, IEnumerable<TaskStepEntity> allSteps)
    {
        if (step.Status == StepStatus.DONE)
        {
            return false;
        }

        return allSteps.Any(previousStep =>
            previousStep.OrderIndex < step.OrderIndex
            && previousStep.IsRequired
            && previousStep.Status != StepStatus.DONE);
    }

    public static bool IsTaskDone(IEnumerable<TaskStepEntity> steps)
    {
        var materializedSteps = steps.ToList();
        return materializedSteps.Count > 0 && materializedSteps.All(step => step.Status == StepStatus.DONE);
    }

    public static int GetCurrentStepNumber(IEnumerable<TaskStepEntity> steps)
    {
        var orderedSteps = SortSteps(steps).ToList();
        if (orderedSteps.Count == 0)
        {
            return 0;
        }

        var firstOpenStepIndex = orderedSteps.FindIndex(step => step.Status == StepStatus.OPEN);
        return firstOpenStepIndex < 0 ? orderedSteps.Count : firstOpenStepIndex + 1;
    }
}
