# DevTrack — Project Log

A complete record of what's been built, what's pending, and every real issue hit and resolved along the way. Kept detailed on purpose — this is as valuable as the code itself for interview prep and onboarding anyone new to the project.

---

## 1. Architecture Overview

Layered ASP.NET Core MVC solution:

```
DevTrack.sln
 ├─ src/
 │   ├─ DevTrack.Web            → MVC project (Controllers, Views, Identity UI, wwwroot)
 │   ├─ DevTrack.Api            → JWT-protected Web API and local AI task-planning endpoint
 │   ├─ DevTrack.Core           → Domain entities + interfaces (zero dependencies)
 │   ├─ DevTrack.Application    → Services / business logic (depends only on Core)
 │   └─ DevTrack.Infrastructure → EF Core, DbContexts, repository implementations
 └─ tests/
     └─ DevTrack.Tests          → xUnit service and local AI-provider tests
```

**Dependency rule:** Core depends on nothing. Application and Infrastructure depend on Core. Web/Api depend on Application (services) and Infrastructure (DI registration only).

**Two databases, deliberately separated:**
- `DevTrackDb` — Projects, Sprints, Work Items (`DevTrackDbContext`)
- `DevTrackIdentityDb` — Users, Roles, Identity tables (`DevTrackIdentityDbContext`)

Chosen over a single merged database specifically so authentication is its own bounded context — different security requirements, independently scalable/replaceable, smaller blast radius if one side is ever compromised.

**Domain model:** `ProjectEntity` → has many `Sprint` → has many `WorkItem` (with `WorkItemStatus` enum: Backlog, InProgress, InReview, Done).

---

## 2. What's Been Completed

### 2.1 Core CRUD (Projects, Sprints, Work Items)
- Repository pattern: generic `IRepository<T>`/`Repository<T>`, plus entity-specific repos (`ISprintRepository`, `IWorkItemRepository`) with eager-loading methods (`GetAllWithSprintAsync`, `GetByIdWithSprintAsync`, etc.)
- Full CRUD controllers and Razor views for all three entities
- Dropdowns for Sprint→Project and WorkItem→Sprint relationships, correctly repopulated on validation failure

### 2.2 Services / Application Layer
- `DevTrack.Application` project created, sitting between Core and Web/Infrastructure
- `IWorkItemService`/`WorkItemService`: business logic layer separate from repositories
- Real business rule: a WorkItem cannot be created/moved into a Sprint that has already ended (`Sprint.EndDate < DateTime.Now`), enforced server-side by re-fetching the Sprint rather than trusting client-submitted data
- Controller catches `InvalidOperationException` specifically (not generic `Exception`) and surfaces it via `ModelState.AddModelError`, so business-rule violations show as clean validation messages instead of crashing

### 2.3 Authentication (ASP.NET Core Identity)
- Two-DbContext setup: `ApplicationUser : IdentityUser` and `DevTrackIdentityDbContext : IdentityDbContext<ApplicationUser>`, both living in `DevTrack.Infrastructure.Identity`
- Scaffolded Identity UI (Login/Register/Logout/Manage pages) under `Areas/Identity/Pages`
- Custom `DevTrackEmailSender : IEmailSender` — logs emails to console instead of sending real ones (no provider configured); used for password reset links during local dev
- New registrations auto-confirm `EmailConfirmed = true` immediately after successful `CreateAsync`, avoiding the need for a real email provider
- `_LoginPartial.cshtml` wired into `_Layout.cshtml` nav bar

### 2.4 Roles & Authorization
- `Admin` and `TeamMember` roles seeded automatically on every app startup (idempotent — checks `RoleExistsAsync` first)
- Global authorization policy: `RequireAuthenticatedUser()` applied via `AddControllersWithViews(options => options.Filters.Add(new AuthorizeFilter(policy)))` — every MVC controller requires login by default, no per-controller `[Authorize]` needed
- Named policy `"AdminOnly"` (`RequireRole("Admin")`) applied specifically to Delete actions across Projects, Sprints, and WorkItems controllers
- `HomeController`'s `Index`/`Privacy` explicitly marked `[AllowAnonymous]` to stay public
- Identity's own Login/Register/ForgotPassword/etc. pages explicitly allowed anonymous via `AllowAnonymousToAreaPage` conventions in `Program.cs` (the global filter would otherwise lock these out too)
- Verified end-to-end: logged-out → redirects to login; Admin → full access; authenticated-but-no-role → 403 Access Denied on Delete specifically

### 2.5 Visual Design
- Custom dark-slate theme (`site.css`) — deliberately avoided generic AI-default palettes; built a token system: `--bg`, `--surface`, `--accent` (cyan), `--success`, `--warning`, `--danger`, plus `Space Grotesk`/`Inter`/`JetBrains Mono` type roles
- Signature element: a status-flow legend on the homepage built directly from the real `WorkItemStatus` enum (Backlog → In Progress → In Review → Done), not generic decoration
- Status pills applied consistently in the WorkItems table, colored per status
- Tables wrapped in `.table-card` styling (rounded, bordered, hover states) instead of flat Bootstrap defaults
- Action buttons color-coded by intent: neutral (Details), cyan (Edit), red (Delete) — consistent across Projects/Sprints/WorkItems

### 2.6 Azure Deployment
- **Azure SQL:** `devtrack-sql-muzni` server created (India South Central), two databases (`DevTrackDb`, `DevTrackIdentityDb`) on the free-tier-eligible **Basic** DTU tier, Locally-redundant backup storage, firewall configured for both Azure services and the developer's own IP
- **Migrations run against Azure** directly from the local machine via `dotnet ef database update --context <Name>` pointed at Azure connection strings (temporarily set via User Secrets) — schema created identically to local
- **App Service:** `devtrack-web` created manually (Linux, Free F1 tier, .NET 10 LTS, no Application Insights, no Defender) in the same resource group/region as the SQL server
- **Connection strings** configured as Azure App Service Connection Strings (`DevTrackConnection`, `IdentityConnection`), separate from local User Secrets — production secrets never touch git
- **Cost alerts** set up on both the SQL resource group and (recommended) the App Service, budget threshold ~$10, alerting at 80%
- **Published successfully** from Visual Studio via Zip Deploy, after resolving several deployment-specific issues (full list below)
- Live site confirmed working: home page, registration, login, and CRUD all functioning against the Azure-hosted databases
- **GitHub Actions pipeline:** `.github/workflows/ci-cd.yml` restores, builds, and tests every pull request and push to `main`; successful non-PR runs publish and deploy `DevTrack.Web` to `devtrack-web` after the `AZURE_WEBAPP_PUBLISH_PROFILE` repository secret is configured

### 2.7 Local AI Task Planning
- `POST /api/ai/task-suggestions` accepts an authenticated user's task title, description, and Sprint ID and returns 3–7 structured subtask suggestions with assumptions and whole-hour estimates
- Local-first `OllamaTaskPlanningService` calls Ollama at `http://localhost:11434`; no cloud API key or per-request charge is required
- Suggestions are never persisted automatically: users review them, then create accepted work items through the existing API flow
- Output is constrained by a JSON schema and validated before being returned to the client; the provider is covered by a simulated HTTP-response xUnit test

---

## 3. What's Pending

1. **AI-assist UI and endpoint tests** — add an MVC review/accept screen and controller-level API tests for JWT authorization, validation, and unavailable-model responses
2. **Enable CD secret** — the workflow is committed, but deployment activates only after `AZURE_WEBAPP_PUBLISH_PROFILE` is added to GitHub repository secrets
3. **API hosting** — provision a separate App Service for `DevTrack.Api`, then add a distinct deployment job and publish-profile secret
4. **`Microsoft.OpenApi` NU1903 vulnerability warning** — flagged early on, not yet addressed; check `dotnet list package --vulnerable` and update to a patched version
5. **Azure roles/data seeding** — roles seed automatically on first app run against Azure (same startup code), but no sample Projects/Sprints/WorkItems have been manually re-entered into the live Azure database yet
6. **Application Insights / monitoring** — intentionally skipped during App Service creation to avoid a regional conflict; could be added later as its own deliberate step, in a compatible region
7. **Base repository refactor consideration** — currently `[Authorize(Policy = "AdminOnly")]` is repeated across three controllers' Delete actions; acceptable at current scale, but worth knowing a base controller class or more centralized policy structure is the next step if this grows
8. **Real email provider** — `DevTrackEmailSender` still just logs to console; no SendGrid/SMTP wired up for actual email delivery in production

---

## 4. Errors Encountered & How They Were Solved

Organized roughly in the order they came up.

### 4.1 `dotnet add reference` silently failing
**Symptom:** CLI reported `"Project already has a reference to..."` even when the `.csproj` file, checked directly, had no `<ProjectReference>` at all.
**Fix:** Never trust the CLI's claim blindly — verify with `type <path-to-csproj>`. When the reference was genuinely missing, added it manually by editing the `.csproj` XML directly. This happened more than once (Application→Core, Web→Application) and each time the direct-file-check approach resolved it.

### 4.2 Interface visibility (`internal` vs `public`)
**Symptom:** `IWorkItemRepository`/similar interfaces defined in `Core` weren't visible from `Infrastructure`/`Web` (separate assemblies).
**Fix:** Interfaces meant to be implemented/consumed across project boundaries must be `public`, not `internal` (the default scaffold sometimes generated `internal`).

### 4.3 Missing DI registrations
**Symptom:** `Unable to resolve service for type 'IWorkItemRepository'` at runtime.
**Fix:** DI registration in `Program.cs` is explicit — adding an interface + implementation to the codebase does not automatically register it. Added `builder.Services.AddScoped<IWorkItemRepository, WorkItemRepository>();` (and equivalents for every new repository/service added since).

### 4.4 Wrong method overload on Delete
**Symptom:** Controller called `_workItemService.DeleteAsync(workItem)` when the service method actually expected `DeleteAsync(int id)`.
**Fix:** Corrected the call; also simplified the controller since the service already handled the "does it exist" null check internally — no need to fetch the entity first just to delete it.

### 4.5 Eager loading gaps (null navigation properties)
**Symptom:** `Sprint.Project` and `WorkItem.Sprint` showed as `null` in views despite data existing, because EF Core doesn't lazy-load by default.
**Fix:** Added dedicated repository methods (`GetAllWithSprintAsync`, `GetByIdWithSprintAsync`) using `.Include()`, and used those specifically wherever a view needed to display the related entity's data.

### 4.6 Identity scaffolding created duplicate classes
**Symptom:** Scaffolding the Identity UI generated its own `ApplicationUser` and (empty) `DevTrackIdentityDbContext` inside `DevTrack.Web/Data/`, conflicting with the already-correct versions in `DevTrack.Infrastructure.Identity`. Resulted in `CS0104: 'ApplicationUser' is ambiguous`.
**Fix:** Deleted the duplicate scaffolded files, then bulk Find & Replace across the whole `Areas/Identity` folder (`DevTrack.Web.Data` → `DevTrack.Infrastructure.Identity`) to fix ~100 resulting `CS0234`/`CS0246` errors across every scaffolded page and `_ViewImports.cshtml` file at once.

### 4.7 Duplicate Identity registration
**Symptom:** `AddDefaultIdentity<ApplicationUser>()` and `AddIdentity<ApplicationUser, IdentityRole>()` were both called in `Program.cs`, registering Identity services twice.
**Fix:** Removed `AddDefaultIdentity` entirely, kept only `AddIdentity<TUser, TRole>()` since role support was required.

### 4.8 Missing `IEmailSender` registration
**Symptom:** `InvalidOperationException: Unable to resolve service for type 'IEmailSender'` on the Register page.
**Cause:** `AddDefaultIdentity` auto-registers a no-op email sender internally; `AddIdentity` does not.
**Fix:** Built a custom `DevTrackEmailSender : IEmailSender`, registered manually. First attempt named it `NoOpEmailSender`, which collided with a same-named internal class inside `Microsoft.AspNetCore.Identity.UI` — renamed to `DevTrackEmailSender` to resolve the ambiguity.

### 4.9 Layout not found for Identity pages
**Symptom:** `InvalidOperationException: The layout view '/Pages/Shared/_Layout.cshtml' could not be located.`
**Fix:** `Areas/Identity/Pages/_ViewStart.cshtml` needed to explicitly point at `/Views/Shared/_Layout.cshtml` (the app's real layout) rather than relying on Identity's default convention path. Resolved once the file was actually saved and the app rebuilt.

### 4.10 Forgot Password silently doing nothing
**Symptom:** No reset email/log line appeared after requesting a password reset.
**Cause:** The scaffolded `ForgotPassword` page silently skips sending anything if `EmailConfirmed` is `false` (a deliberate security measure — doesn't reveal account existence). Freshly registered accounts defaulted to unconfirmed.
**Fix:** Manually set `EmailConfirmed = 1` via SQL for existing accounts; permanently fixed by adding `user.EmailConfirmed = true; await _userManager.UpdateAsync(user);` inside the `if (result.Succeeded)` block of `Register.cshtml.cs`, so new accounts auto-confirm.

### 4.11 `sqlcmd` QUOTED_IDENTIFIER error
**Symptom:** `UPDATE`/`DELETE` against `AspNetUsers`/`AspNetUserRoles` via `sqlcmd -Q` failed: *"SET options are incorrect for use with indexed views..."*
**Cause:** `sqlcmd` defaults to `QUOTED_IDENTIFIER OFF`; Identity's filtered unique indexes require it `ON`.
**Fix:** Prefixed affected commands with `SET QUOTED_IDENTIFIER ON;`.

### 4.12 Global auth filter broke Identity's own login/register pages
**Symptom:** Infinite redirect loop on `/Identity/Account/Login` (`HTTP 414 URI Too Long`) once a global `AuthorizeFilter` requiring authentication was added.
**Cause:** MVC filters registered via `AddControllersWithViews(options => options.Filters.Add(...))` also apply to Razor Pages — including Identity's own Login/Register pages, which then required login to reach the login page.
**Fix:** Explicitly allowed anonymous access to the specific Identity entry-point pages via `options.Conventions.AllowAnonymousToAreaPage("Identity", "/Account/Login")` (and similarly for Register, ForgotPassword, ResetPassword, AccessDenied, etc.) inside `AddRazorPages()`, while leaving `/Account/Manage/*` pages correctly protected.

### 4.13 Copilot suggested over-broad authorization
**Symptom:** GitHub Copilot suggested adding `[Authorize(Roles = "Admin")]` to Create and Edit actions on WorkItems, not just Delete.
**Fix:** Reviewed and rejected the suggestion — TeamMembers should be able to do routine CRUD; only Delete should be Admin-gated. Kept the class-level `[Authorize]` (any logged-in user) and scoped the role restriction to Delete only.

### 4.14 Footer "bug" that wasn't a bug
**Symptom:** Footer appeared to float mid-page on longer forms, seemingly matching the old ASP.NET MVC template's `position: absolute` footer CSS.
**Real cause:** Windows display scaling was set to 150%, visually compressing the viewport and making the layout *look* broken. At 100% scale, the layout was correct all along.
**Lesson:** Always verify OS display scale before debugging a layout issue — CSS changes made to "fix" this were unnecessary and reverted.

### 4.15 Laptop reformat — lost local dev environment
**Symptom:** After a full laptop reformat, `dotnet ef` commands failed with *"Could not execute because the specified command or file was not found."*
**Fix:** `dotnet-ef` is a separate global tool, not bundled with the SDK — reinstalled via `dotnet tool install --global dotnet-ef`. Code was safe (pushed to GitHub); databases were rebuilt from scratch by re-running `dotnet ef database update --context <Name>` for both contexts. Confirmed the value of migrations-as-code: full schema recreation required zero manual SQL.

### 4.16 "More than one DbContext was found"
**Symptom:** `dotnet ef database update` without specifying which context to target failed since the project has two (`DevTrackDbContext`, `DevTrackIdentityDbContext`).
**Fix:** Always include `--context <ContextName>` on every EF Core CLI command in this project.

### 4.17 Azure SQL Database creation — "Conflict" status
**Symptom:** First database creation attempt (alongside server creation, via the "Create a resource" wizard) failed with a generic `Conflict` deployment status, while the server itself was created successfully.
**Fix:** Created the database directly from the existing server's own page (`+ Create database`) instead of bundling it with server creation — avoided whatever race condition caused the conflict.

### 4.18 Wrong SQL pricing tier for a Free Trial subscription
**Symptom:** Hyperscale/General Purpose Serverless tiers showed no cost estimate (`--` USD) and a red banner: *"Free Trial subscriptions can provision Basic, Standard S0 through S3... only."*
**Fix:** Switched to the DTU-based **Basic** tier — the cheapest option genuinely supported on a Free Trial subscription (~$4.90/month per database, not literally free, but minimal and predictable).

### 4.19 Networking toggles not saving during database creation wizard
**Symptom:** "Allow Azure services..." and "Add current client IP" toggles appeared stuck on "No" during the second database's creation wizard, even after clicking "Yes."
**Fix:** These are server-level settings; edited them directly on the server's own **Networking** blade after creation instead of relying on the per-database wizard.

### 4.20 Client IP change blocking Azure SQL connection
**Symptom:** `Client with IP address 'X' is not allowed to access the server` when running migrations from a different network session than when the firewall rule was created.
**Fix:** Dynamic IPs change over time (ISP-dependent) — re-added the current IP via the server's Networking blade (`Add your client IPv4 address` one-click button) whenever this recurred. Documented as an expected, recurring friction point, not a one-time fix.

### 4.21 Pasted the wrong value into Azure App Service Connection Strings
**Symptom:** The full local terminal command (`dotnet user-secrets set "ConnectionStrings:..." "Server=..."`) was pasted into the Value field, instead of just the raw connection string.
**Fix:** Edited both entries to contain only the connection string itself (starting from `Server=tcp:...`), since Azure's Connection Strings UI already handles the key-name mapping via the separate "Name" field.

### 4.22 Application Insights regional restriction blocking auto-deploy wizard
**Symptom:** `Template Validation Failed — The provided location 'indiasouthcentral' is not available for resource type 'microsoft.insights/components'.`
**Cause:** The GitHub-import "Create Web App" wizard ties Application Insights to the resource group's region, and India South Central isn't a supported region for that specific resource type.
**Fix:** Abandoned the GitHub auto-deploy wizard for now; switched to manual App Service creation, explicitly disabling Application Insights during setup to sidestep the regional conflict entirely. CI/CD deferred to a later, separate task.

### 4.23 Publish failed — 401 Unauthorized (Zip Deploy)
**Symptom:** Visual Studio's Publish (Zip Deploy) failed with `HTTP status code 'Unauthorized'`.
**Cause:** New App Services disable "Basic Auth Publishing Credentials" by default; Zip Deploy relies on this.
**Fix:** Enabled **SCM Basic Auth Publishing Credentials** under the App Service's Configuration → General settings, then applied.

### 4.24 Still 401 after enabling Basic Auth — stale credentials
**Symptom:** Same 401 error persisted immediately after enabling Basic Auth.
**Cause:** Visual Studio's existing publish profile had cached older/incorrect credentials, predating the Basic Auth change.
**Fix:** Downloaded a fresh `.PublishSettings` file from the Azure portal (App Service Overview → Download publish profile) and imported it as a new profile in Visual Studio.

### 4.25 Wrong deployment protocol — Web Deploy fails on Linux
**Symptom:** After importing the fresh publish profile, publish failed differently: *"Could not connect to the remote computer... make sure Web Deploy is installed and Web Management Service is started."*
**Cause:** The imported `.PublishSettings` file defaulted to **Web Deploy (MSDeploy)**, which requires IIS-style infrastructure — fundamentally incompatible with a **Linux** App Service.
**Fix:** Deleted both stale profiles; created a fresh one via Visual Studio's Azure-authenticated publish wizard (Target → Azure → App Service (Linux) → selected `devtrack-web` directly, not via file import). This correctly generated a **Zip Deploy** profile, the right protocol for Linux App Services.

### 4.26 Successful deployment
After resolving 4.23–4.25, `dotnet publish` via the new Zip Deploy profile succeeded. Live site confirmed reachable, with registration, login, and CRUD verified working against the Azure-hosted databases.

---

## 5. Key Recurring Lessons

- **Verify, don't trust tool output blindly** — the CLI, Visual Studio wizards, and even error banners occasionally lied or were misleading (stale `dotnet add reference` messages, networking toggles not persisting, a "footer bug" that was actually a display-scale illusion). Checking the actual file/state directly was the reliable path every time.
- **DI is explicit** — nothing gets wired up automatically just because the code exists; every service/repository needs a deliberate registration line.
- **Read the exact error text** — nearly every Azure deployment failure was solved by reading the specific wording of the error (region names, "Web Management Service," "Unauthorized" vs a connection failure) rather than guessing.
- **Migrations-as-code paid off directly** — surviving a full laptop reformat with zero data-modeling work lost, purely because schema lived in checked-in migration files, not just a local database.
- **Security-by-default settings can look like bugs** — `EmailConfirmed` blocking password resets, Basic Auth being disabled by default, the global auth filter also catching Identity's own pages — all were intentional platform defaults that needed to be understood, not just worked around.
