using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using ReleaseManagement.Application.Approvals;
using ReleaseManagement.Application.Authorization;
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

        return services;
    }
}
