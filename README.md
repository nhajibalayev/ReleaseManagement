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

## On-prem Azure DevOps Server + Windows / AD login (nh-nk)

Typical corporate setup (on-prem collection, same AD credentials as the PC):

```json
{
  "WindowsAuth": {
    "Enabled": true,
    "AllowLocalLogin": true,
    "DefaultRole": "ProductOwner"
  },
  "AzureAd": {
    "Enabled": false
  },
  "AzureDevOps": {
    "Enabled": true,
    "OrganizationUrl": "https://devops.nh-nk.az/DefaultCollection",
    "Project": "<project-name>",
    "PersonalAccessToken": "<pat>",
    "WorkItemType": "Task",
    "ApiVersion": "7.1",
    "UseWindowsCredentials": false,
    "RequireProjectAccessToCreate": true
  }
}
```

Store secrets with User Secrets (do not commit PAT):

```powershell
cd src/ReleaseManagement.Web
dotnet user-secrets init
dotnet user-secrets set "AzureDevOps:PersonalAccessToken" "<pat>"
dotnet user-secrets set "AzureDevOps:Project" "<project-name>"
dotnet user-secrets set "WindowsAuth:Enabled" "true"
dotnet user-secrets set "AzureDevOps:Enabled" "true"
```

Notes:
- Login page shows **Sign in with Windows / Active Directory** (Negotiate). Works best on a domain-joined Windows machine / IIS.
- API calls to DevOps use the PAT by default (reliable for Hangfire background jobs). Set `UseWindowsCredentials: true` only if the app pool / process identity should call ADO with Windows auth instead.
- If create-release access checks fail on older servers, try `ApiVersion` `6.0` or `5.1`.

## Azure AD SSO + Azure DevOps Services (cloud, optional)

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

With cloud SSO enabled, create-release can check project access via the user's OAuth token (PAT remains a fallback).

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
