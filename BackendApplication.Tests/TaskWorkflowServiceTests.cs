using BackendApplication.Api.Domain.Entities;
using BackendApplication.Api.Domain.Enums;
using BackendApplication.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace BackendApplication.Tests;

public sealed class TaskWorkflowServiceTests
{
    [Fact]
    public async Task CompleteManualStepAsync_ReturnsDependencyFailure_WhenPrecedingRequiredStepIsOpen()
    {
        await using var testDb = await SqliteTestDb.CreateAsync();
        var (task, _, secondStep) = await SeedTwoManualStepsAsync(testDb.DbContext, firstStepDone: false);

        var service = new TaskWorkflowService(testDb.DbContext);
        var result = await service.CompleteManualStepAsync(task.AssignedUserId, secondStep.Id);

        Assert.False(result.Succeeded);
        Assert.Equal(ManualStepCompletionFailure.DependencyNotMet, result.Failure);
    }

    [Fact]
    public async Task CompleteManualStepAsync_SetsTaskDone_WhenFinalStepIsCompleted()
    {
        await using var testDb = await SqliteTestDb.CreateAsync();
        var (task, _, secondStep) = await SeedTwoManualStepsAsync(testDb.DbContext, firstStepDone: true);

        var service = new TaskWorkflowService(testDb.DbContext);
        var result = await service.CompleteManualStepAsync(task.AssignedUserId, secondStep.Id);

        var storedTask = await testDb.DbContext.Tasks
            .Include(currentTask => currentTask.Steps)
            .SingleAsync(currentTask => currentTask.Id == task.Id);

        Assert.True(result.Succeeded);
        Assert.False(result.AlreadyDone);
        Assert.Equal(TaskState.DONE, storedTask.Status);
        Assert.All(storedTask.Steps, step => Assert.Equal(StepStatus.DONE, step.Status));
    }

    [Fact]
    public async Task CompleteManualStepAsync_ReturnsAutomatedStepFailure_ForAutomatedStep()
    {
        await using var testDb = await SqliteTestDb.CreateAsync();
        var task = new UserTaskEntity
        {
            Title = "Onboarding",
            Description = "Task",
            AssignedUserId = "user-1"
        };

        task.Steps.Add(new TaskStepEntity
        {
            Title = "Auto Step",
            Description = "Step",
            Type = StepType.Automated,
            ModuleKey = "profile",
            IsRequired = true,
            OrderIndex = 1
        });

        testDb.DbContext.Tasks.Add(task);
        await testDb.DbContext.SaveChangesAsync();

        var automatedStep = task.Steps.Single();
        var service = new TaskWorkflowService(testDb.DbContext);
        var result = await service.CompleteManualStepAsync(task.AssignedUserId, automatedStep.Id);

        Assert.False(result.Succeeded);
        Assert.Equal(ManualStepCompletionFailure.AutomatedStep, result.Failure);
    }

    [Fact]
    public async Task ProcessModuleEntryCreatedAsync_CompletesMatchingUnlockedAutomatedStep()
    {
        await using var testDb = await SqliteTestDb.CreateAsync();
        var task = new UserTaskEntity
        {
            Title = "Onboarding",
            Description = "Task",
            AssignedUserId = "user-1"
        };

        task.Steps.Add(new TaskStepEntity
        {
            Title = "Manual Setup",
            Description = "Step",
            Type = StepType.Manual,
            Status = StepStatus.DONE,
            IsRequired = true,
            OrderIndex = 1
        });

        task.Steps.Add(new TaskStepEntity
        {
            Title = "Create Profile",
            Description = "Step",
            Type = StepType.Automated,
            ModuleKey = "profile",
            IsRequired = true,
            OrderIndex = 2
        });

        testDb.DbContext.Tasks.Add(task);
        await testDb.DbContext.SaveChangesAsync();

        var service = new TaskWorkflowService(testDb.DbContext);
        var result = await service.ProcessModuleEntryCreatedAsync(task.AssignedUserId, "profile");

        var automatedStep = await testDb.DbContext.Steps
            .SingleAsync(step => step.TaskId == task.Id && step.OrderIndex == 2);

        Assert.True(result.Succeeded);
        Assert.Equal(StepStatus.DONE, automatedStep.Status);
    }

    [Fact]
    public async Task ProcessModuleEntryCreatedAsync_ReturnsNoMatch_WhenAutomatedStepIsLocked()
    {
        await using var testDb = await SqliteTestDb.CreateAsync();
        var task = new UserTaskEntity
        {
            Title = "Onboarding",
            Description = "Task",
            AssignedUserId = "user-1"
        };

        task.Steps.Add(new TaskStepEntity
        {
            Title = "Manual Setup",
            Description = "Step",
            Type = StepType.Manual,
            Status = StepStatus.OPEN,
            IsRequired = true,
            OrderIndex = 1
        });

        task.Steps.Add(new TaskStepEntity
        {
            Title = "Create Profile",
            Description = "Step",
            Type = StepType.Automated,
            ModuleKey = "profile",
            IsRequired = true,
            OrderIndex = 2
        });

        testDb.DbContext.Tasks.Add(task);
        await testDb.DbContext.SaveChangesAsync();

        var service = new TaskWorkflowService(testDb.DbContext);
        var result = await service.ProcessModuleEntryCreatedAsync(task.AssignedUserId, "profile");

        Assert.False(result.Succeeded);
        Assert.Equal(ModuleEntryProcessingFailure.NoMatchingActiveStep, result.Failure);
    }

    private static async Task<(UserTaskEntity Task, TaskStepEntity FirstStep, TaskStepEntity SecondStep)> SeedTwoManualStepsAsync(
        DbContext dbContext,
        bool firstStepDone)
    {
        var task = new UserTaskEntity
        {
            Title = "Onboarding",
            Description = "Task",
            AssignedUserId = "user-1"
        };

        var firstStep = new TaskStepEntity
        {
            Title = "First Step",
            Description = "Step",
            Type = StepType.Manual,
            IsRequired = true,
            OrderIndex = 1,
            Status = firstStepDone ? StepStatus.DONE : StepStatus.OPEN
        };

        var secondStep = new TaskStepEntity
        {
            Title = "Second Step",
            Description = "Step",
            Type = StepType.Manual,
            IsRequired = true,
            OrderIndex = 2
        };

        task.Steps.Add(firstStep);
        task.Steps.Add(secondStep);

        dbContext.Set<UserTaskEntity>().Add(task);
        await dbContext.SaveChangesAsync();

        return (task, firstStep, secondStep);
    }
}
