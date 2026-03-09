using BackendApplication.Api.Domain.Enums;

namespace BackendApplication.Api.Contracts.Admin;

public sealed class ForceStepStatusRequest
{
    public StepStatus Status { get; init; }
}
