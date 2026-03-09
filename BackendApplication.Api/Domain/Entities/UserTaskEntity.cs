using BackendApplication.Api.Domain.Enums;

namespace BackendApplication.Api.Domain.Entities;

public sealed class UserTaskEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string AssignedUserId { get; set; } = string.Empty;
    public TaskState Status { get; set; } = TaskState.OPEN;
    public bool IsStatusForced { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<TaskStepEntity> Steps { get; set; } = new List<TaskStepEntity>();
}
