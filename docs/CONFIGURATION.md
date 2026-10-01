# Configuration & secrets

Single source of truth in code: `src/ReleaseManagement.Web/Secrets/SecretKeys.cs`.
Startup (`Program.cs`) runs `AddCorporateVault()` → `AddAppSecrets()`: Vault secrets are merged
into configuration, then every key below is validated and its origin (appsettings / env / Vault)
is written to the log — values are never logged.

## Where each value lives

| Config key (as the app reads it) | Env / Vault name | Source | Required | What it is |
|---|---|---|---|---|
| `ConnectionStrings:DefaultConnection` | `ConnectionStrings__DefaultConnection` | **Vault** | yes | PostgreSQL: `Host=...;Port=5432;Database=...;Username=...;Password=...` |
| `WindowsAuth:Enabled` | `WindowsAuth__Enabled` | ConfigMap | no | `true` = AD user/password sign-in |
| `WindowsAuth:Mode` | `WindowsAuth__Mode` | ConfigMap | no | `Real` or `Mock` (any user, password `Mock!123`) |
| `WindowsAuth:Domain` | `WindowsAuth__Domain` | ConfigMap | no | AD DNS domain, e.g. `test-namiq.local` |
| `WindowsAuth:LdapServer` | `WindowsAuth__LdapServer` | ConfigMap | Linux + Real | Domain controller FQDN, e.g. `mcpads01.test-namiq.local` |
| `WindowsAuth:LdapUseSsl` | `WindowsAuth__LdapUseSsl` | ConfigMap | no | `true` = LDAPS 636 (recommended) |
| `WindowsAuth:DefaultRole` | `WindowsAuth__DefaultRole` | ConfigMap | no | Role on first sign-in, default `ProductOwner` |
| `AzureDevOps:Enabled` | `AzureDevOps__Enabled` | ConfigMap | no | `true` = integrate with DevOps Server |
| `AzureDevOps:OrganizationUrl` | `AzureDevOps__OrganizationUrl` | ConfigMap | if ADO Real | `https://devops.jora-life.com/DefaultCollection` |
| `AzureDevOps:Project` | `AzureDevOps__Project` | ConfigMap | if ADO Real | `Release Management Board` (real spaces) |
| `AzureDevOps:WorkItemType` | `AzureDevOps__WorkItemType` | ConfigMap | no | `Task` / `Release` |
| `AzureDevOps:ApiVersion` | `AzureDevOps__ApiVersion` | ConfigMap | no | `6.0` works on-prem |
| `AzureDevOps:PersonalAccessToken` | `AzureDevOps__PersonalAccessToken` | **Vault** | no | Normally empty — signed-in user's AD creds (NTLM) are used |
| `Email:Password` | `Email__Password` | **Vault** | no | SMTP password, only if `Email:Enabled=true` |

"Required" is checked for the active mode only: DemoMode needs nothing, Mock AD/ADO need no addresses.
Missing required keys stop the application at startup with a message naming them.

## Vault (production, Kubernetes)

Client: corporate package `PL.Common.SecretVault`, called as
`builder.Services.AddVaultWithKubernetes(builder.Configuration)` in
`Secrets/CorporateVault.cs`. It is compiled in only with `-p:CorpVault=true`
(Docker: `--build-arg CORP_VAULT=true`, compose: `CORP_VAULT=true` in `.env`),
because the package exists only on the internal NuGet mirror (`NuGet.Config`).

Library settings live in `appsettings.json` → `Vault` section (`Address`, `Role`, `SecretPath`).
Key names inside that section must match what `PL.Common.SecretVault` reads — confirm with the
owners of the package and adjust the section if needed.

### Checklist for DevOps

1. Vault path for the project, e.g. `secret/releasemanagement/<env>`; put the **Vault** rows above there,
   using the *Env / Vault name* column as the key (same format as other PL.* services).
2. Vault role bound to the application's Kubernetes service account, policy = read that path only.
3. ConfigMap with the *ConfigMap* rows above (env var names from the same column).
4. Pass the `Vault` section values (address / role / path) as env: `Vault__Address`, `Vault__Role`, `Vault__SecretPath`.
5. Network: namespace → Vault (443), PostgreSQL (5432), domain controller (389/636), DevOps Server (443).

## Local (Docker Desktop / Visual Studio)

No Vault: `AddCorporateVault()` is skipped in `Development` and is a no-op when not compiled in.
Values come from `.env` (`docker-compose.yml` maps them to the env names above) or `dotnet user-secrets`.
Set `ASPNETCORE_ENVIRONMENT=Development` in `.env` for local runs.

## Adding a new secret

1. Add a `SecretKey` to `SecretKeys.cs` (+ to `All`), and a property to `AppSecrets` if code needs it typed.
2. Add the row to the table above.
3. Add the variable to `.env.example` and `docker-compose.yml`.
4. Tell DevOps to create it in Vault / ConfigMap.
