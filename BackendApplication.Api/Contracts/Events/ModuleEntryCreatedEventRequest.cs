using System.ComponentModel.DataAnnotations;

namespace BackendApplication.Api.Contracts.Events;

public sealed class ModuleEntryCreatedEventRequest
{
    [Required]
    [MaxLength(100)]
    public string UserId { get; init; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string ModuleKey { get; init; } = string.Empty;
}
