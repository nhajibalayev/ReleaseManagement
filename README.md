# Release Management Platform

Corporate release lifecycle management built with ASP.NET Core MVC, Razor
Views, PostgreSQL, and Azure DevOps integration.

## Current status

Stages 1–5 are complete: domain, application, infrastructure, and MVC UI
(dashboard, release wizard/details, approvals, notifications, calendar,
reports, and admin screens) are in place. Stage 6 covers expanded automated
tests; Stage 7 covers Docker and final documentation polish.

## Local database

Default connection string expects PostgreSQL on `localhost:5432`.

```powershell
dotnet ef database update --project src/ReleaseManagement.Infrastructure --startup-project src/ReleaseManagement.Web
dotnet run --project src/ReleaseManagement.Web
```

Development seed users (password `ChangeMe!123`): `admin`, `po`, `rm`, `pentest`, `infosec`, `business`, `devops`, `auditor`.

## Azure AD SSO + Azure DevOps (delegated tokens)

Configure via **User Secrets** on `ReleaseManagement.Web` (do not commit secrets):

```json
{
  "AzureAd": {
    "Enabled": true,
    "TenantId": "<tenant-guid>",
    "ClientId": "<app-client-id>",
    "ClientSecret": "<app-secret>",
    "AllowLocalLogin": true,
    "DefaultRole": "ProductOwner"
  },
  "AzureDevOps": {
    "Enabled": true,
    "OrganizationUrl": "https://dev.azure.com/<org>",
    "Project": "<project>",
    "WorkItemType": "Task",
    "RequireProjectAccessToCreate": true
  }
}
```

Entra ID app registration needs:
- Redirect URI: `https://localhost:7171/signin-oidc` (and prod URL)
- API permission: Azure DevOps `user_impersonation` (+ admin consent)
- Optional Graph: `User.Read`

With SSO enabled, create-release checks that the signed-in user can access the configured ADO project. Work item create/update uses the user's OAuth access token (PAT remains an optional fallback).

See [Stage 1 Architecture](docs/ARCHITECTURE.md) for the solution design,
workflow, entities, controllers, views, and package plan.

## Build

Prerequisite: .NET 10 SDK.

```powershell
dotnet restore
dotnet build --no-restore
dotnet test --no-build
```

## Projects

- `ReleaseManagement.Domain`: dependency-free business model and rules.
- `ReleaseManagement.Application`: use cases and abstractions.
- `ReleaseManagement.Infrastructure`: persistence and external adapters.
- `ReleaseManagement.Web`: ASP.NET Core MVC composition root and UI.
- `ReleaseManagement.Domain.Tests`: domain unit tests.
- `ReleaseManagement.Application.Tests`: application unit tests.
- `ReleaseManagement.IntegrationTests`: end-to-end application tests.
