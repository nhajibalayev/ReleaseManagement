using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using ReleaseManagement.Application.Approvals;
using ReleaseManagement.Application.Authorization;
using ReleaseManagement.Application.Governance;
using ReleaseManagement.Application.Planning;
using ReleaseManagement.Application.PostRelease;
using ReleaseManagement.Application.Readiness;
using ReleaseManagement.Application.Releases;

namespace ReleaseManagement.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddScoped<IReleaseAuthorizationService, ReleaseAuthorizationService>();
        services.AddScoped<IReleaseAppService, ReleaseAppService>();
        services.AddScoped<IReleaseWorkflowService, ReleaseWorkflowService>();
        services.AddScoped<IApprovalService, ApprovalService>();
        services.AddScoped<IReadinessService, ReadinessService>();
        services.AddScoped<IPostReleaseService, PostReleaseService>();
        services.AddScoped<IPlanningService, PlanningService>();
        services.AddScoped<IGovernanceService, GovernanceService>();

        return services;
    }
}
