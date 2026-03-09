using BackendApplication.Api.Domain.Enums;

namespace BackendApplication.Api.Contracts.User;

public sealed record ManualStepCompletionResponse(
    Guid TaskId,
    Guid StepId,
    TaskState TaskStatus,
    StepStatus StepStatus,
    bool AlreadyDone);
