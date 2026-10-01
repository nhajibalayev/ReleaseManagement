using Hangfire;
using ReleaseManagement.Infrastructure;
using ReleaseManagement.Infrastructure.Persistence;
using ReleaseManagement.Web.Secrets;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // 1. Vault (production only; no-op locally) adds its secrets to builder.Configuration.
    // 2. SecretsLoader validates every key from Secrets/SecretKeys.cs and logs where each came from.
    builder.AddCorporateVault();
    builder.AddAppSecrets();

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "ReleaseManagement")
        .WriteTo.Console()
        .WriteTo.File(
            path: "logs/release-management-.log",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 14));

    builder.Services.AddControllersWithViews(options =>
    {
        var policy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();
        options.Filters.Add(new Microsoft.AspNetCore.Mvc.Authorization.AuthorizeFilter(policy));
    });
    builder.Services.AddInfrastructure(builder.Configuration);
    var demoMode = builder.Configuration.GetValue<bool>("DemoMode");
    var hangfireEnabled = builder.Configuration.GetValue<bool>("Hangfire:Enabled");

    var app = builder.Build();

    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Home/Error");
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseSerilogRequestLogging();
    app.UseRouting();
    app.UseSession();
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapStaticAssets();
    app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}")
        .WithStaticAssets();

    app.MapHealthChecks("/health");
    app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = _ => false
    });
    app.MapHealthChecks("/health/ready");

    if (!demoMode && hangfireEnabled)
    {
        app.UseHangfireDashboard("/hangfire", new Hangfire.DashboardOptions
        {
            Authorization = [new Hangfire.Dashboard.LocalRequestsOnlyAuthorizationFilter()]
        });
    }

    try
    {
        using var scope = app.Services.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<DevelopmentDataSeeder>();
        await seeder.SeedAsync();
    }
    catch (Exception seedException)
    {
        Log.Warning(seedException, "Database seed/migration failed. Ensure PostgreSQL is running.");
    }

    if (!demoMode && hangfireEnabled)
    {
        DependencyInjection.MapInfrastructureJobs(builder.Configuration);
    }

    await app.RunAsync();
}
catch (Exception exception)
{
    Log.Fatal(exception, "Application terminated unexpectedly.");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}

public partial class Program;
