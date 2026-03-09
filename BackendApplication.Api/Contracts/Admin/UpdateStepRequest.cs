using System.ComponentModel.DataAnnotations;
using BackendApplication.Api.Domain.Enums;

namespace BackendApplication.Api.Contracts.Admin;

public sealed class UpdateStepRequest
{
    [Required]
    [MaxLength(200)]
    public string Title { get; init; } = string.Empty;

    [Required]
    [MaxLength(2000)]
    public string Description { get; init; } = string.Empty;

    [MaxLength(200)]
    public string? Icon { get; init; }

    public StepType Type { get; init; } = StepType.Manual;

    public bool IsRequired { get; init; } = true;

    [Range(1, int.MaxValue)]
    public int OrderIndex { get; init; } = 1;

    [MaxLength(100)]
    public string? ModuleKey { get; init; }
}
