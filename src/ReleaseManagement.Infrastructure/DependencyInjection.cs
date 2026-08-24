using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ReleaseManagement.Application;
using ReleaseManagement.Application.Abstractions;
using ReleaseManagement.Application.Authorization;
using ReleaseManagement.Domain.Constants;
using ReleaseManagement.Infrastructure.AzureDevOps;
using ReleaseManagement.Infrastructure.Files;
using ReleaseManagement.Infrastructure.Identity;
using ReleaseManagement.Infrastructure.Jobs;
using ReleaseManagement.Infrastructure.Notifications;
using ReleaseManagement.Infrastructure.Options;
using ReleaseManagement.Infrastructure.Outbox;
using ReleaseManagement.Infrastructure.Persistence;
using ReleaseManagement.Infrastructure.Services;

namespace ReleaseManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<AzureDevOpsOptions>(configuration.GetSection(AzureDevOpsOptions.SectionName));
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));
        services.Configure<FileStorageOptions>(configuration.GetSection(FileStorageOptions.SectionName));
        services.Configure<SeedOptions>(configuration.GetSection(SeedOptions.SectionName));

        var demoMode = configuration.GetValue<bool>("DemoMode");
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            if (demoMode)
            {
                options.UseInMemoryDatabase("ReleaseManagementDemo");
                return;
            }

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "Connection string 'DefaultConnection' is missing.");
            }

            options.UseNpgsql(connectionString);
        });

        services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<ApplicationDbContext>());

        services
            .AddIdentity<AppIdentityUser, IdentityRole<Guid>>(options =>
            {
                options.Password.RequiredLength = 8;
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedAccount = false;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/Account/Login";
            options.AccessDeniedPath = "/Account/AccessDenied";
            options.SlidingExpiration = true;
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.SameAsRequest;
        });

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<IReleaseNumberGenerator, ReleaseNumberGenerator>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IOutboxWriter, OutboxWriter>();
        services.AddScoped<IFileStorageService, LocalFileStorageService>();
        services.AddScoped<IEmailSender, LoggingEmailSender>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<DevelopmentDataSeeder>();
        services.AddTransient<OutboxProcessorJob>();
        services.AddTransient<TemporaryFileCleanupJob>();

        services.AddHttpClient<IAzureDevOpsService, AzureDevOpsService>((provider, client) =>
            {
                var options = configuration
                    .GetSection(AzureDevOpsOptions.SectionName)
                    .Get<AzureDevOpsOptions>() ?? new AzureDevOpsOptions();
                AzureDevOpsHttpClientConfigurator.Configure(client, options);
            })
            .AddStandardResilienceHandler();

        if (!demoMode)
        {
            services.AddHangfire(config => config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UsePostgreSqlStorage(options =>
                    options.UseNpgsqlConnection(connectionString!)));

            services.AddHangfireServer();
        }

        var healthChecks = services.AddHealthChecks()
            .AddCheck<FileStorageHealthCheck>("file-storage");

        if (!demoMode)
        {
            healthChecks.AddNpgSql(connectionString!, name: "postgresql");
        }

        services.AddAuthorization(options =>
        {
            options.AddPolicy(
                AuthorizationPolicies.CanCreateRelease,
                policy => policy.RequireRole(RoleNames.ProductOwner, RoleNames.Administrator));
            options.AddPolicy(
                AuthorizationPolicies.CanReviewRelease,
                policy => policy.RequireRole(RoleNames.ReleaseManager, RoleNames.Administrator));
            options.AddPolicy(
                AuthorizationPolicies.CanPerformPentest,
                policy => policy.RequireRole(RoleNames.Pentest, RoleNames.Administrator));
            options.AddPolicy(
                AuthorizationPolicies.CanPerformInfoSecReview,
                policy => policy.RequireRole(RoleNames.InfoSec, RoleNames.Administrator));
            options.AddPolicy(
                AuthorizationPolicies.CanApproveBusiness,
                policy => policy.RequireRole(RoleNames.BusinessApprover, RoleNames.Administrator));
            options.AddPolicy(
                AuthorizationPolicies.CanDeployRelease,
                policy => policy.RequireRole(RoleNames.DevOps, RoleNames.Administrator));
            options.AddPolicy(
                AuthorizationPolicies.CanManageSystem,
                policy => policy.RequireRole(RoleNames.Administrator));
            options.AddPolicy(
                AuthorizationPolicies.CanViewAuditLogs,
                policy => policy.RequireRole(
                    RoleNames.Administrator,
                    RoleNames.Auditor,
                    RoleNames.ReleaseManager));
        });

        services.AddApplication();
        return services;
    }

    public static void MapInfrastructureJobs()
    {
        RecurringJob.AddOrUpdate<OutboxProcessorJob>(
            "outbox-processor",
            job => job.ProcessAsync(),
            Cron.Minutely);

        RecurringJob.AddOrUpdate<TemporaryFileCleanupJob>(
            "temporary-file-cleanup",
            job => job.ExecuteAsync(),
            Cron.Daily);
    }
}
