using BackendApplication.Api.Domain.Enums;

namespace BackendApplication.Api.Contracts.Events;

public sealed record ModuleEntryCreatedEventResponse(
    Guid TaskId,
    Guid StepId,
    TaskState TaskStatus);
