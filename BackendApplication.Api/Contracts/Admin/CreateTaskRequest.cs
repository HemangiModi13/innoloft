using System.ComponentModel.DataAnnotations;

namespace BackendApplication.Api.Contracts.Admin;

public sealed class CreateTaskRequest
{
    [Required]
    [MaxLength(200)]
    public string Title { get; init; } = string.Empty;

    [Required]
    [MaxLength(2000)]
    public string Description { get; init; } = string.Empty;

    [MaxLength(200)]
    public string? Icon { get; init; }

    [Required]
    [MaxLength(100)]
    public string AssignedUserId { get; init; } = string.Empty;
}
