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

## On-prem Azure DevOps Server + Active Directory login (nh-nk)

Sign in with the same AD username/password you use for
`https://devops.nh-nk.az`. The app validates against Active Directory and
calls Azure DevOps Server REST APIs **as that user** (no PAT required).

```json
{
  "WindowsAuth": {
    "Enabled": true,
    "Domain": "NH-NK",
    "EnableNegotiate": false,
    "AllowLocalLogin": false,
    "DefaultRole": "ProductOwner"
  },
  "AzureAd": {
    "Enabled": false
  },
  "AzureDevOps": {
    "Enabled": true,
    "OrganizationUrl": "https://devops.nh-nk.az/DefaultCollection",
    "Project": "<project-name>",
    "WorkItemType": "Release",
    "ApiVersion": "7.1",
    "AreaPath": "",
    "IterationPath": "",
    "RequireProjectAccessToCreate": true
  }
}
```

`AreaPath` / `IterationPath` — чтобы work item попал на нужный team/release board.
В DevOps открой карточку на борде → смотри Area / Iteration (например `MyProject\\Release Team`).

User Secrets example:

```powershell
cd src/ReleaseManagement.Web
dotnet user-secrets init
dotnet user-secrets set "WindowsAuth:Enabled" "true"
dotnet user-secrets set "WindowsAuth:Domain" "NH-NK"
dotnet user-secrets set "WindowsAuth:AllowLocalLogin" "false"
dotnet user-secrets set "AzureDevOps:Enabled" "true"
dotnet user-secrets set "AzureDevOps:OrganizationUrl" "https://devops.nh-nk.az/DefaultCollection"
dotnet user-secrets set "AzureDevOps:Project" "<project-name>"
```

How it works:
1. Login form accepts `DOMAIN\user` (or `user` + configured Domain) and AD password.
2. Credentials are validated against Active Directory.
3. For DevOps API / board-access checks, the app builds a Basic auth header from **your** AD credentials for that session.
4. Create/update work-item outbox jobs capture that authorization at submit time.

Optional: set `WindowsAuth:EnableNegotiate` to `true` for browser integrated Windows login (no password form; DevOps calls then need process/Windows credentials separately).

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
    "WorkItemType": "Release",
    "RequireProjectAccessToCreate": true
  }
}
```

Entra ID app registration needs:
- Redirect URI: `https://localhost:7171/signin-oidc` (and prod URL)
- API permission: Azure DevOps `user_impersonation` (+ admin consent)
- Optional Graph: `User.Read`

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
