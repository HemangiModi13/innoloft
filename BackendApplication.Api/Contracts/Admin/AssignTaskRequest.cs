using System.ComponentModel.DataAnnotations;

namespace BackendApplication.Api.Contracts.Admin;

public sealed class AssignTaskRequest
{
    [Required]
    [MaxLength(100)]
    public string AssignedUserId { get; init; } = string.Empty;
}
