# DevTrack

A layered ASP.NET Core MVC sprint & task tracker, built as a hands-on learning project to gain production-level experience with .NET Core, EF Core, and ASP.NET Core Identity.

## 🎯 Overview

DevTrack is a Jira-style sprint and task tracker for small teams - Projects, Sprints, and Work Items, with role-based access control (Admin / Team Member). It's built with a clean, layered architecture rather than a single monolithic project, so business logic stays testable and independent of the database and UI.

## 🛠️ Tech Stack

- **Language:** C#
- **Framework:** ASP.NET Core MVC (net10.0)
- **Data access:** Entity Framework Core, SQL Server (LocalDB for local development)
- **Auth:** ASP.NET Core Identity, with a separate Identity database from the domain database
- **Frontend:** Razor Views, Bootstrap 5
- **Patterns:** Repository pattern, Service (Application) layer, Dependency Injection, Policy-based authorization

## 🏗️ Architecture

```
DevTrack.sln
 ├─ src/
 │   ├─ DevTrack.Web            → MVC project (Controllers, Views, Identity UI, wwwroot)
 │   ├─ DevTrack.Api            → Web API project
 │   ├─ DevTrack.Core           → Domain entities + interfaces (zero dependencies)
 │   ├─ DevTrack.Application    → Services / business logic (depends only on Core)
 │   └─ DevTrack.Infrastructure → EF Core, DbContexts, repository implementations
 └─ tests/
     └─ DevTrack.Tests          → xUnit
```

**Dependency rule:** `Core` depends on nothing. `Application` and `Infrastructure` depend on `Core`. `Web`/`Api` depend on `Application` (for services) and `Infrastructure` (for DI registration only). This keeps domain logic swappable and unit-testable without needing a real database.

**Two databases, deliberately separated:**
- `DevTrackDb` - Projects, Sprints, Work Items (via `DevTrackDbContext`)
- `DevTrackIdentityDb` - Users, Roles, and all ASP.NET Core Identity tables (via `DevTrackIdentityDbContext`)

Authentication is kept in its own bounded context, separate from domain data, rather than merged into one database.

## 📋 Prerequisites

Before cloning, make sure the following are installed:

- **.NET SDK** (net10.0) - verify with `dotnet --version`
- **SQL Server Express LocalDB** - usually bundled with Visual Studio's ASP.NET/web workload. Verify with `sqllocaldb info`. If that command isn't recognized, reinstall/repair Visual Studio with the ASP.NET and web development workload checked.
- **EF Core CLI tool** (`dotnet-ef`) - **not** included with the SDK by default; see setup step 3 below.
- **Visual Studio 2022+** (recommended) or any editor with C# support
- **Git**

## 🚀 Getting Started

### 1. Clone the repository

```bash
git clone https://github.com/MZ-MUZNI/DevTrack.git
cd DevTrack
```

### 2. Restore NuGet packages

```bash
dotnet restore
```

This reads every project's `.csproj` and downloads all required packages automatically - nothing needs to be installed manually. Key packages worth knowing about, since they explain a lot of the app's behavior:

| Package | Project | Purpose |
|---|---|---|
| `Microsoft.EntityFrameworkCore.SqlServer` | Infrastructure, Web | EF Core's SQL Server provider |
| `Microsoft.EntityFrameworkCore.Design` | Web | Powers `dotnet ef` commands (migrations, database update) |
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore` | Infrastructure | ASP.NET Core Identity's EF Core store |

### 3. Install the EF Core CLI tool (global, one-time per machine)

`dotnet ef` is **not** part of the base .NET SDK — it's a separate global tool that must be installed on every machine you develop on, including after a fresh OS install or a new laptop:

```bash
dotnet tool install --global dotnet-ef
```

If it's already installed but outdated:
```bash
dotnet tool update --global dotnet-ef
```

Verify it worked:
```bash
dotnet ef --version
```

### 4. Create the databases

DevTrack uses **two separate `DbContext`s**, so migrations must be applied to each one individually using the `--context` flag. Run both of these from the repository root:

```bash
# Domain data: Projects, Sprints, Work Items
dotnet ef database update --project src\DevTrack.Infrastructure --startup-project src\DevTrack.Web --context DevTrackDbContext

# Identity data: Users, Roles
dotnet ef database update --project src\DevTrack.Infrastructure --startup-project src\DevTrack.Web --context DevTrackIdentityDbContext
```

This creates `DevTrackDb` and `DevTrackIdentityDb` on your local `(localdb)\MSSQLLocalDB` instance, with the schema built entirely from the migration files already checked into this repo — no manual table creation needed.

### 5. Run the application

```bash
dotnet run --project src\DevTrack.Web
```

On first run, the app automatically seeds two roles (`Admin`, `TeamMember`) into the Identity database — you'll see `INSERT INTO [AspNetRoles]` in the console output confirming this.

The app will be available at the URL printed in the console (e.g. `http://localhost:5236`).

### 6. Register an account and assign yourself Admin

1. Navigate to `/Identity/Account/Register` and create an account.
2. New accounts are auto-confirmed on registration, so you can log in immediately.
3. To assign yourself the `Admin` role (required to delete Projects/Sprints/Work Items), run:

```bash
sqlcmd -S "(localdb)\MSSQLLocalDB" -d DevTrackIdentityDb -Q "SET QUOTED_IDENTIFIER ON; INSERT INTO AspNetUserRoles (UserId, RoleId) SELECT u.Id, r.Id FROM AspNetUsers u, AspNetRoles r WHERE u.Email = 'your-email-here' AND r.Name = 'Admin'"
```

## 🔑 Roles & Permissions

| Action | Anonymous | Logged in (any role) | Admin |
|---|:---:|:---:|:---:|
| View Home / Privacy | ✅ | ✅ | ✅ |
| View / Create / Edit Projects, Sprints, Work Items | ❌ | ✅ | ✅ |
| Delete Projects, Sprints, Work Items | ❌ | ❌ | ✅ |

Authorization is enforced with a global policy (login required by default across all MVC controllers) plus a named `"AdminOnly"` policy applied to Delete actions, both configured in `Program.cs`.

## 🩺 Troubleshooting

### `Could not execute because the specified command or file was not found` (running `dotnet ef ...`)
The EF Core CLI tool isn't installed on this machine. Run `dotnet tool install --global dotnet-ef` (see Prerequisites/Setup step 3), then retry.

### `More than one DbContext was found. Specify which one to use.`
DevTrack has two `DbContext`s (`DevTrackDbContext` and `DevTrackIdentityDbContext`). Every `dotnet ef` command must include `--context <ContextName>` to specify which one you mean — see setup step 4 for exact commands.

### `sqlcmd` fails with `UPDATE/DELETE failed because ... 'QUOTED_IDENTIFIER'`
LocalDB's Identity tables include a filtered index that requires `QUOTED_IDENTIFIER ON`. Prefix any `sqlcmd -Q` command that modifies `AspNetUsers`/`AspNetUserRoles` with `SET QUOTED_IDENTIFIER ON;`, as shown in the commands above.

### Password reset email never arrives / "Forgot password" seems to do nothing
Check the `EmailConfirmed` column for that user:
```bash
sqlcmd -S "(localdb)\MSSQLLocalDB" -d DevTrackIdentityDb -Q "SET QUOTED_IDENTIFIER ON; SELECT Email, EmailConfirmed FROM AspNetUsers"
```
If it's `0`, the scaffolded ForgotPassword flow silently skips sending anything — a security convention, not a bug. New registrations are auto-confirmed by default in this project, so this should only affect older/manually-seeded accounts. To confirm one manually:
```bash
sqlcmd -S "(localdb)\MSSQLLocalDB" -d DevTrackIdentityDb -Q "SET QUOTED_IDENTIFIER ON; UPDATE AspNetUsers SET EmailConfirmed = 1 WHERE Email = 'the-email'"
```
There's no real email provider configured - `DevTrackEmailSender` (`src/DevTrack.Web/Services/DevTrackEmailSender.cs`) logs emails to the console instead of sending them. Watch the terminal running `dotnet run` for the reset link after triggering "Forgot your password?".

### Unable to resolve service for type `IWorkItemRepository` / similar DI error
A new interface/implementation pair was added but never registered in `Program.cs`. DI is explicit - every repository/service needs its own `builder.Services.AddScoped<TInterface, TImplementation>()` line.

### Fresh machine / reinstalled OS and LocalDB data is gone
LocalDB's actual data files aren't part of this git repo (by design - only schema-defining migration code is checked in). After cloning on a new machine, just re-run setup steps 3–6 above to rebuild both databases from the migrations. You will need to re-register any user accounts and re-enter sample data, since that lived only in the local database files.

## 🗺️ Project Status

Actively developed as a learning project. Current feature set: full CRUD across Projects/Sprints/Work Items, layered architecture with a Service layer, ASP.NET Core Identity with role-based authorization. Planned next: Web API + DTOs, automated tests, CI/CD, and Azure deployment.

## 👤 Author

**MZ-MUZNI**
- GitHub: [@MZ-MUZNI](https://github.com/MZ-MUZNI)
