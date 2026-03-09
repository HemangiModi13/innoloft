using BackendApplication.Api.Domain.Enums;

namespace BackendApplication.Api.Contracts.User;

public sealed record UserStepResponse(
    Guid Id,
    string Title,
    string Description,
    string? Icon,
    StepStatus Status,
    StepType Type,
    bool IsRequired,
    int OrderIndex,
    string? ModuleKey,
    bool IsLocked,
    bool CanCompleteManually);
