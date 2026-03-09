using BackendApplication.Api.Domain.Enums;

namespace BackendApplication.Api.Contracts.User;

public sealed record UserTaskSummaryResponse(
    Guid Id,
    string Title,
    string Description,
    string? Icon,
    TaskState Status,
    int TotalSteps,
    int CompletedSteps,
    int RemainingSteps,
    int LockedSteps,
    Guid? NextStepId,
    string? NextStepTitle);
