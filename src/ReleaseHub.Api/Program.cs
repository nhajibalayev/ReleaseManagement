using Microsoft.AspNetCore.Authentication;
using Microsoft.OpenApi.Models;
using ReleaseHub.Api.Auth;
using ReleaseHub.Application;
using ReleaseHub.Application.Abstractions.Auth;
using ReleaseHub.Infrastructure;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, services, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console());

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "ReleaseHub API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "Opaque",
        Description = "SSO access token issued by the Identity Service."
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("WebApp", policy => policy
        .WithOrigins("http://localhost:5173")
        .AllowAnyHeader()
        .AllowAnyMethod());
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserAccessor, HttpContextCurrentUserAccessor>();

builder.Services
    .AddAuthentication(SsoAuthenticationOptions.DefaultScheme)
    .AddScheme<SsoAuthenticationOptions, SsoAuthenticationHandler>(SsoAuthenticationOptions.DefaultScheme, _ => { });
builder.Services.AddAuthorization();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("WebApp");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }
