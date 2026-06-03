using System.Net.Http.Headers;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ReleaseHub.Application.Abstractions.AzureDevOps;
using ReleaseHub.Application.Abstractions.Auth;
using ReleaseHub.Application.Abstractions.Email;
using ReleaseHub.Application.Abstractions.Persistence;
using ReleaseHub.Application.Options;
using ReleaseHub.Infrastructure.AzureDevOps;
using ReleaseHub.Infrastructure.Email;
using ReleaseHub.Infrastructure.Persistence;
using ReleaseHub.Infrastructure.Sso;

namespace ReleaseHub.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));
        services.Configure<SsoOptions>(configuration.GetSection(SsoOptions.SectionName));
        services.Configure<EmailServiceOptions>(configuration.GetSection(EmailServiceOptions.SectionName));
        services.Configure<AzureDevOpsOptions>(configuration.GetSection(AzureDevOpsOptions.SectionName));

        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            var db = sp.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            options.UseNpgsql(db.ConnectionString);
        });
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        services.AddHttpClient<ISsoClient, SsoClient>();

        services.AddHttpClient<IEmailClient, HttpEmailClient>((sp, client) =>
        {
            var opts = sp.GetRequiredService<IOptions<EmailServiceOptions>>().Value;
            if (!string.IsNullOrEmpty(opts.BaseUrl)) client.BaseAddress = new Uri(opts.BaseUrl);
            if (!string.IsNullOrEmpty(opts.ApiKey)) client.DefaultRequestHeaders.Add("X-Api-Key", opts.ApiKey);
        });

        services.AddHttpClient<IAzureDevOpsClient, AzureDevOpsClient>((sp, client) =>
        {
            var opts = sp.GetRequiredService<IOptions<AzureDevOpsOptions>>().Value;
            if (!string.IsNullOrEmpty(opts.BaseUrl))
            {
                var baseUri = opts.BaseUrl.EndsWith("/") ? opts.BaseUrl : opts.BaseUrl + "/";
                if (!string.IsNullOrEmpty(opts.Collection))
                {
                    baseUri += opts.Collection.TrimEnd('/') + "/";
                }
                client.BaseAddress = new Uri(baseUri);
            }
            if (!string.IsNullOrEmpty(opts.PersonalAccessToken))
            {
                var token = Convert.ToBase64String(Encoding.ASCII.GetBytes(":" + opts.PersonalAccessToken));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", token);
            }
        });

        return services;
    }
}
