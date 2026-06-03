using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReleaseHub.Application.ReleaseTasks;

namespace ReleaseHub.Api.Controllers;

[ApiController]
[Route("api/release-tasks")]
[Authorize]
public sealed class ReleaseTasksController : ControllerBase
{
    private readonly IReleaseTaskService _service;

    public ReleaseTasksController(IReleaseTaskService service) => _service = service;

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ReleaseTaskDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ReleaseTaskDto>>> List(CancellationToken ct)
    {
        var items = await _service.ListAsync(ct);
        return Ok(items);
    }
}
