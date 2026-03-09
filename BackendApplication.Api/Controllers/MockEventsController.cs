using BackendApplication.Api.Contracts.Events;
using BackendApplication.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace BackendApplication.Api.Controllers;

[ApiController]
[Route("api/mock/events")]
public sealed class MockEventsController(TaskWorkflowService workflowService) : ControllerBase
{
    [HttpPost("module-entry-created")]
    public async Task<ActionResult<ModuleEntryCreatedEventResponse>> ModuleEntryCreated(
        [FromBody] ModuleEntryCreatedEventRequest request,
        CancellationToken cancellationToken)
    {
        var result = await workflowService.ProcessModuleEntryCreatedAsync(
            request.UserId.Trim(),
            request.ModuleKey.Trim(),
            cancellationToken);

        if (!result.Succeeded)
        {
            return NotFound(new { message = "No active automated step matched this user/module event." });
        }

        return Ok(new ModuleEntryCreatedEventResponse(
            result.Task!.Id,
            result.Step!.Id,
            result.Task.Status));
    }
}
