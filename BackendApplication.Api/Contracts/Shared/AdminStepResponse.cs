using BackendApplication.Api.Domain.Enums;

namespace BackendApplication.Api.Contracts.Shared;

public sealed record AdminStepResponse(
    Guid Id,
    string Title,
    string Description,
    string? Icon,
    StepStatus Status,
    StepType Type,
    bool IsRequired,
    int OrderIndex,
    string? ModuleKey);
