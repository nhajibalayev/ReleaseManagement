# CLAUDE.md — Release Management Platform handoff

Use this file as the primary context when continuing work in Claude Code (or any other agent).
Last updated: 2026-09-18 (Procedure v4.0 alignment, uncommitted). Branch: `main`. Remote: `https://github.com/nhajibalayev/ReleaseManagement.git`.

---

## 1. What this project is

Corporate **Release Management Platform** for Paşa Həyat / nh-nk style on-prem environment:

- ASP.NET Core MVC (.NET 10), Razor Views
- Clean Architecture: Domain / Application / Infrastructure / Web
- PostgreSQL + EF Core
- On-prem **Azure DevOps Server** (`https://devops.nh-nk.az/DefaultCollection`)
- **Active Directory** username/password login (same credentials as Windows / DevOps)
- No PAT required for DevOps API when user is signed in with AD

Repo path (author machine): `C:\Users\Namiq\source\repos\nhajibalayev\ReleaseManagement`

---

## 2. Solution layout

```
ReleaseManagement.sln
src/
  ReleaseManagement.Domain/          # entities, enums, workflow rules
  ReleaseManagement.Application/     # services, DTOs, authorization interfaces
  ReleaseManagement.Infrastructure/  # EF, Identity, ADO, AD auth, Hangfire/outbox
  ReleaseManagement.Web/             # MVC controllers + Razor views
tests/
  ReleaseManagement.Domain.Tests/
  ReleaseManagement.Application.Tests/
  ReleaseManagement.IntegrationTests/
docs/
  ARCHITECTURE.md                    # partially OUTDATED (still describes old linear workflow)
```

Key domain files:

- `Domain/Constants/RoleNames.cs`
- `Domain/Enums/ReleaseEnums.cs`, `SupportingEnums.cs`
- `Domain/Rules/ReleaseWorkflowRules.cs`  ← **source of truth for transitions**
- `Application/Releases/ReleaseWorkflowService.cs`
- `Application/Releases/ReleaseStatusDisplay.cs`
- `Application/Approvals/ApprovalService.cs`
- `Infrastructure/AzureDevOps/AzureDevOpsService.cs`
- `Infrastructure/AzureDevOps/AzureDevOpsAuthHandler.cs`  ← NTLM/Negotiate
- `Infrastructure/AzureDevOps/AzureDevOpsReleaseSyncService.cs`
- `Infrastructure/Identity/ActiveDirectoryAuthenticator.cs`
- `Web/Controllers/ReleasesController.cs`
- `Web/Views/Releases/Details.cshtml`, `Print.cshtml`

---

## 3. Current workflow (Procedure v4.0 readiness model — implemented 2026-09-18)

**Readiness-checklist / link-first model.** The RM coordinates; evidence owners set their own
control statuses; hard gates block Ready / Closed (§8.1). Old RM→QA/InfoSec/Risk/ChapterLead
routing is gone from `ReleaseManagerReview` (legacy statuses remain only for old rows).

```
Draft
  → Submit (checks §8.2 minimum record + freeze §6.3) → Submitted → (auto) ReleaseManagerReview
      → RM "Accept record and start readiness" → ReadinessInProgress
           (controls created: Source / Product / QA / Security / DB / Ops / Recovery / Window+Comms;
            owner roles set Ready | NotRequired+justification | ReadyWithApprovedException (security) | Blocked)
      → RM "Mark ready" → ReadyForRelease   [gate: all applicable controls closed, pre-release comms,
                                              Expedited needs PO Ready, Major needs forecast + formal recovery]
      → DevOps deploy → DeploymentInProgress → Deployed
           (PostReleaseValidation row auto-created; TO records technical, PO records business validation)
      → RM/TO "Start stabilization" → Stabilization   [gate: validation passed]
      → RM "Close" → Closed   [gate: stabilization end reached, Outcome set, PIR completed if triggered]
      (DeploymentFailed / RollbackInProgress / RolledBack as before; each auto-opens a PIR)
```

Rules: `ReleaseWorkflowRules.cs` (transitions), `ReleaseReadinessRules.cs` (controls + gates),
`ReleaseClassificationRules.cs` (§4.1 category). Gates are applied in
`ReleaseWorkflowService.EnsureGateAsync`; side effects in `ApplyPostTransitionEffectsAsync`.

Legacy statuses still exist for old data: `PentestReview`, `BusinessApproval`, `QaReview`,
`InfoSecReview`, `RiskReview`, `ChapterLeadReview`, `Approved`.

## 4. Roles (ASP.NET Identity — NOT auto from AD)

```
Administrator, ProductOwner, ReleaseManager,
QA, InfoSec, Risk, ChapterLead,
TechnicalOwner, DBA, ITOperations,   # procedure v4.0 §2 (seed: techowner / dba / itops)
Pentest, BusinessApprover,           # legacy
DevOps, Auditor
```

Seed gives `rm` BOTH ReleaseManager and TechnicalOwner (one person does both today).

How roles work:

- Stored in Identity (`AspNetRoles` / `AspNetUserRoles`).
- System does **not** infer RM/InfoSec from AD group or job title.
- First AD login provisions user with `WindowsAuth:DefaultRole` (usually `ProductOwner`).
- Admin **Users** screen is **read-only** (lists users + roles). **No UI to assign roles yet.**
- Demo seed users (when `Seed:Enabled`): `admin`, `po`, `rm`, `qa`, `infosec`, `risk`, `chapterlead`, `pentest`, `business`, `devops`, `auditor` — password `ChangeMe!123`.

Workflow checks `CurrentUser.IsInRole(...)`. Status has `CurrentResponsibleRole`.

---

## 5. Azure DevOps integration (working on corp)

### Auth

- On-prem IIS often rejects Basic with AD password → use **NTLM/Negotiate** via `AzureDevOpsAuthHandler` + `NetworkCredential` from signed-in session store.
- Credentials stored in protected session after AD login (`IAzureDevOpsUserCredentialStore`).

### Behavior

- On **Submit**: work item is created ONLY when `AzureDevOps:CreateWorkItemOnSubmit=true`
  (default false — procedure §1.3 is link-first: the record links existing WI/PR/pipeline via `ReleaseReference`).
- Comments posted in app → sync to work item **Discussion** (Comments API; History fallback) — commit `4570743`.
- Status updates can append History.
- Project name must use real spaces: `Release Management Board` (not literal `%20` in config).

### Known-good corp config pattern

```json
"WindowsAuth": {
  "Enabled": true,
  "Domain": "<AD_DOMAIN>",
  "AllowLocalLogin": false,
  "DefaultRole": "ProductOwner"
},
"Hangfire": { "Enabled": false },
"AzureDevOps": {
  "Enabled": true,
  "OrganizationUrl": "https://devops.nh-nk.az/DefaultCollection",
  "Project": "Release Management Board",
  "WorkItemType": "Release",
  "ApiVersion": "6.0",
  "RequireProjectAccessToCreate": true
}
```

Notes:

- User confirmed **ApiVersion `6.0`** works in their environment (repo default in appsettings may still say `7.1`).
- Custom work item type `Release` can be picky on required fields; `Task` may be easier if create fails.
- Work item is created on **Submit**, not on draft save.

---

### Mock mode (non-corporate machines)

`appsettings.Development.json` now sets `AzureDevOps:Mode="Mock"` (in-memory `MockAzureDevOpsService`,
project access always allowed) and `WindowsAuth:Mode="Mock"` (`MockActiveDirectoryAuthenticator`:
any user name + password `Mock!123` signs in and is provisioned as ProductOwner; seeded local
accounts still work with `ChangeMe!123`). On the corp laptop override via user-secrets: `Mode="Real"`.

## 6. What the app can do today

| Area | Capability |
|------|------------|
| Releases | Wizard create/edit draft, submit, details, my/all lists |
| Approvals | RM routing; QA/InfoSec/Risk/ChapterLead review; pending queue |
| Comments | Threaded dialogue (Reply); sync to ADO Discussion |
| Print | Print/PDF-friendly release page |
| Attachments | Local file storage upload/download |
| Deployment | Manual start/complete/fail/rollback; pipeline URL/build fields |
| Notifications | In-app + email outbox (email often disabled) |
| Admin | Products create/list; Environments list; Users list (read-only) |
| Audit | Status history + audit log |
| Auth | Local Identity and/or AD password login |

| Procedure v4.0 | Category Minor/Normal/Major (criteria flags), Planned/Expedited, planned window, planned maintenance, link-first references, readiness matrix, communications log, reschedule, freeze periods + exceptions, quarterly forecast, post-release validation, stabilization, outcome, PIR with actions, KPI page (§9), CBAR 7.28 pack (`/Releases/Compliance/{id}`) |

Screens added: `Forecasts`, `Freezes`, `Reviews`, `Reports` (KPI), `Releases/Compliance`;
`Approvals/Index` is now "My readiness work"; calendar shows §3.2 columns + freezes.

### Explicitly missing / weak

- No admin UI to assign roles or manage users deeply
- Track is fixed to Application (Infrastructure intake via Service Desk was explicitly descoped)
- Incident / Service Desk Change IDs are manual references (no Service Desk integration)
- Retention ≥ 5 years is a governance policy, not enforced by the platform
- IT Governance role not modelled (KPI page is for RM/Admin/Auditor)
- Deployment is manual status, not pipeline-driven
- `docs/ARCHITECTURE.md` still describes **old linear** Pentest→InfoSec→Business path — **do not trust it for workflow**

---

## 7. Corporate procedure document

Gap analysis (Azerbaijani): `Release_Procedure_v4_vs_Platform_Gap_AZ 2.xlsx` — 26 rows on sheet 1;
the 21 rows marked "Prosedura uyğunlaşdırılmalıdır" were implemented on 2026-09-18 (see §3/§6).
Rows 4/6/7 (Technical Owner / IT Ops / IT Governance = "clarify") resolved as: TO = separate role,
same person as RM today; ITOperations role added but TO may close operational readiness;
IT Governance not modelled. Row 1 (Infrastructure track) and row 23 (retention) intentionally skipped.

Migration: the EF migration for these changes must be generated on a machine with the SDK:
`dotnet ef migrations add ProcedureV4Alignment --project src/ReleaseManagement.Infrastructure --startup-project src/ReleaseManagement.Web`
(the app applies migrations at startup through the seeder).

## 8. Important bugs already fixed (do not reintroduce)

1. **DbUpdateConcurrencyException on Submit** — EF treated new `ReleaseStatusHistory` as UPDATE; PostgreSQL `xmin` mapped as concurrency token. Fix: remove xmin mapping; `ForceAdded` for new history; detach/reload patterns. Commits around `2c47c5b`, `4632330`, etc.
2. **ADO 401 with Basic auth** on on-prem — switched to NTLM (`cd33497`).
3. **Hangfire** stale locks / not needed for sync — disabled by default; inline ADO sync on submit (`7e24029`).
4. **Project name `%20`** — use real spaces in config.
5. Create release open to any signed-in user (product access gate for create relaxed).

---

## 9. How to run locally

Prerequisites: .NET 10 SDK, PostgreSQL.

```powershell
cd C:\Users\Namiq\source\repos\nhajibalayev\ReleaseManagement
dotnet ef database update --project src/ReleaseManagement.Infrastructure --startup-project src/ReleaseManagement.Web
dotnet run --project src/ReleaseManagement.Web
```

Corp laptop note: Group Policy / AppLocker may block `dotnet.exe` or runs from Downloads (“This program is blocked by group policy”). Use IT-approved Visual Studio / SDK install and repo under allowed path (not Downloads zip). Prefer `git pull` of `main`, not stale zip.

User secrets example for corp:

```powershell
cd src/ReleaseManagement.Web
dotnet user-secrets set "WindowsAuth:Enabled" "true"
dotnet user-secrets set "WindowsAuth:Domain" "NH-NK"
dotnet user-secrets set "WindowsAuth:AllowLocalLogin" "false"
dotnet user-secrets set "AzureDevOps:Enabled" "true"
dotnet user-secrets set "AzureDevOps:OrganizationUrl" "https://devops.nh-nk.az/DefaultCollection"
dotnet user-secrets set "AzureDevOps:Project" "Release Management Board"
dotnet user-secrets set "AzureDevOps:ApiVersion" "6.0"
dotnet user-secrets set "Hangfire:Enabled" "false"
```

---

## 10. Recent commits on main (newest first)

```
4570743 Sync release comments into Azure DevOps Discussion.
4d3de90 Replace linear approvals with RM-orchestrated sequential routing.
2c47c5b Eliminate Release xmin concurrency and force-insert status history.
cd33497 Authenticate to on-prem DevOps with NTLM using the signed-in AD user.
4632330 Fix ReleaseStatusHistory concurrency failure on submit.
ba307c2 Default Azure DevOps work item type to Release.
7e24029 Disable Hangfire by default and sync Azure DevOps inline on submit.
```

---

## 11. Likely next work (if user asks)

Priority candidates discussed but **not started** unless requested:

1. **Admin UI: assign roles to users** (high practical value — currently missing)
2. Align platform to procedure v4.0 (Track, Category/Mode, readiness checklist, link-first) — large redesign
3. Wire Service Desk Change ID / Incident links
4. Post-release validation + PIR
5. Forecast / freeze / calendar enrichment
6. Stronger ADO payload (PR/pipeline links) instead of create-only WI

Default stance: workflow now follows procedure v4.0; keep it that way. When touching workflow,
update `ReleaseWorkflowRules` + `ReleaseReadinessRules` + tests + `ReleaseStatusDisplay` + views together.

---

## 12. Coding conventions for agents

- Prefer existing patterns; minimal diffs; no drive-by refactors.
- Do not commit/push unless user asks.
- Do not invent PAT-based ADO auth as primary for corp on-prem (use signed-in AD + NTLM).
- Keep Hangfire off unless user wants background jobs again.
- When touching workflow, update `ReleaseWorkflowRules` + tests + `ReleaseStatusDisplay` + `ApprovalService` together.
- User communicates in Russian; product UI is largely English.
- Corporate procedure is Azerbaijani (`Release_Idareetme_Proseduru_v4.0`).

---

## 13. Quick “truth” checklist for the next agent

- [ ] Workflow source of truth = `ReleaseWorkflowRules.cs` (RM sequential), not ARCHITECTURE.md
- [ ] ADO comments = Discussion Comments API path in `AzureDevOpsService.AddCommentAsync`
- [ ] Roles = Identity assignments; admin cannot edit roles in UI yet
- [ ] Corp ApiVersion that worked = `6.0`
- [ ] Project name = spaces, not `%20`
- [ ] Latest change = Procedure v4.0 alignment (readiness model, forecast, freeze, PIR, KPI, CBAR pack)
- [ ] Mock mode for ADO/AD is configured in appsettings.Development.json
