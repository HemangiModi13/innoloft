using BackendApplication.Api.Domain.Enums;

namespace BackendApplication.Api.Contracts.Shared;

public sealed record AdminTaskResponse(
    Guid Id,
    string Title,
    string Description,
    string? Icon,
    string AssignedUserId,
    TaskState Status,
    bool IsStatusForced,
    int TotalSteps,
    int CompletedSteps,
    IReadOnlyList<AdminStepResponse> Steps);
