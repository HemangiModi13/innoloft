using BackendApplication.Api.Domain.Enums;

namespace BackendApplication.Api.Contracts.Admin;

public sealed class ForceTaskStatusRequest
{
    public TaskState Status { get; init; }
}
