using BackendApplication.Api.Domain.Enums;

namespace BackendApplication.Api.Domain.Entities;

public sealed class TaskStepEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TaskId { get; set; }
    public UserTaskEntity Task { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public StepStatus Status { get; set; } = StepStatus.OPEN;
    public StepType Type { get; set; } = StepType.Manual;
    public bool IsRequired { get; set; } = true;
    public int OrderIndex { get; set; }
    public string? ModuleKey { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
