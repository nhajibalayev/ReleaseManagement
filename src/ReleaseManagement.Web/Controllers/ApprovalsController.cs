using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReleaseManagement.Application.Approvals;
using ReleaseManagement.Application.Common;
using ReleaseManagement.Application.DTOs.Approvals;
using ReleaseManagement.Domain.Enums;
using ReleaseManagement.Web.ViewModels;

namespace ReleaseManagement.Web.Controllers;

[Authorize]
public sealed class ApprovalsController : Controller
{
    private readonly IApprovalService _approvalService;

    public ApprovalsController(IApprovalService approvalService)
    {
        _approvalService = approvalService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var items = await _approvalService.GetPendingAsync(cancellationToken);
        var model = new PendingApprovalsPageViewModel
        {
            Items = items.Select(item => new PendingApprovalRowViewModel
            {
                ReleaseId = item.ReleaseId,
                ReleaseNumber = item.ReleaseNumber,
                Title = item.Title,
                ProductName = item.ProductName,
                RequestedDate = item.RequestedDate,
                PlannedReleaseDate = item.PlannedReleaseDate,
                ApprovalType = item.ApprovalType,
                SlaDueDate = item.SlaDueDate
            }).ToArray()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Decide(ApprovalDecisionFormViewModel model, CancellationToken cancellationToken)
    {
        try
        {
            await _approvalService.DecideAsync(
                new ApprovalDecisionRequest
                {
                    ReleaseId = model.ReleaseId,
                    ApprovalType = model.ApprovalType,
                    Decision = model.Decision,
                    Comment = model.Comment
                },
                cancellationToken);

            TempData["Success"] = $"Decision recorded: {model.Decision}.";
        }
        catch (Exception exception) when (exception is BusinessRuleException or ForbiddenException or ConflictException or FluentValidation.ValidationException)
        {
            TempData["Error"] = exception.Message;
        }

        return RedirectToAction("Details", "Releases", new { id = model.ReleaseId });
    }
}
