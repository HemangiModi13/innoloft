using BackendApplication.Api.Domain.Enums;

namespace BackendApplication.Api.Contracts.User;

public sealed record UserTaskDetailsResponse(
    Guid Id,
    string Title,
    string Description,
    string? Icon,
    TaskState Status,
    int TotalSteps,
    int CompletedSteps,
    int CurrentStepNumber,
    IReadOnlyList<UserStepResponse> Steps);
