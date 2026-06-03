using Microsoft.Extensions.DependencyInjection;
using ReleaseHub.Application.ReleaseTasks;

namespace ReleaseHub.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IReleaseTaskService, ReleaseTaskService>();
        return services;
    }
}
