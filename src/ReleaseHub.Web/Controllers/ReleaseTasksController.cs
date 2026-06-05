using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReleaseHub.Application.ReleaseTasks;
using ReleaseHub.Web.Models;

namespace ReleaseHub.Web.Controllers;

[Authorize]
public sealed class ReleaseTasksController : Controller
{
    private readonly IReleaseTaskService _service;

    public ReleaseTasksController(IReleaseTaskService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var items = await _service.ListAsync(ct);
        return View(items);
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        var task = await _service.GetByIdAsync(id, ct);
        if (task is null) return NotFound();
        return View(task);
    }

    [HttpGet]
    public IActionResult Create() => View(new CreateReleaseTaskFormModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateReleaseTaskFormModel form, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(form);

        var created = await _service.CreateAsync(new CreateReleaseTaskRequest
        {
            Title = form.Title,
            Description = form.Description
        }, ct);

        return RedirectToAction(nameof(Details), new { id = created.Id });
    }
}
