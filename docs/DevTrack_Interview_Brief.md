# DevTrack — Interview Preparation Brief

> **Safe to upload:** this document contains no secrets, passwords, connection strings, personal data, or build output. It describes the implementation in the DevTrack repository as reviewed on 17 October 2026.

## How to use this with ChatGPT

Upload this file and use this prompt:

> I built DevTrack. Act as a senior .NET/Azure integration-platform interviewer. Based only on this brief, create a 15-minute laptop walkthrough, then interview me with increasingly difficult follow-up questions. Challenge unclear claims and help me give honest, concise answers. Prioritise architecture, security, testing, delivery, and trade-offs relevant to an Azure-based integration platform.

If a question turns on an exact implementation detail, also upload the named source file rather than uploading the whole repository. Do **not** upload `appsettings*.json`, publish profiles, database files, `.env` files, `bin/`, or `obj/`.

## One-minute description

DevTrack is a Jira-style sprint and work-item tracker for small teams. It provides project, sprint, and work-item CRUD through an ASP.NET Core MVC UI, with an accompanying JWT-protected Web API. The application uses a deliberately layered design: Core contains domain entities and contracts, Application contains business rules, Infrastructure handles EF Core, SQL Server and Identity, and Web/API are delivery hosts. It also has an optional local-AI planner which proposes structured subtasks for a user to review before anything is persisted.

The project was built as a learning project to exercise realistic .NET concerns: separation of concerns, authentication/authorisation, persistence, async I/O, validation, testing, CI/CD, and deployment.

## Technology and architecture

| Layer/project | Responsibility | Key implementation |
| --- | --- | --- |
| `DevTrack.Core` | Dependency-free domain model and contracts | `ProjectEntity`, `Sprint`, `WorkItem`, repositories, `ITaskPlanningService` |
| `DevTrack.Application` | Business orchestration | `WorkItemService` enforces sprint rules |
| `DevTrack.Infrastructure` | EF Core, SQL Server, repositories, Identity and Ollama client | two `DbContext`s, migration files, `OllamaTaskPlanningService` |
| `DevTrack.Web` | MVC/Razor interactive UI | cookie/Identity authentication, policy-protected controllers, AI planner UI |
| `DevTrack.Api` | HTTP API for programmatic access | Identity-backed registration/login, issued JWTs, protected API controllers |
| `DevTrack.Tests` | Focused unit tests | xUnit tests with fake repositories and a stub HTTP handler |

Dependency direction is intentional: Core has no project dependencies; Application and Infrastructure depend on Core; Web and API compose dependencies at the edge. The point is to keep business logic independent of UI and database details, making it easier to test and replace implementations.

### Domain model

`ProjectEntity` has many `Sprint`s; each `Sprint` belongs to a project and has many `WorkItem`s; each work item has a status (`Backlog`, `InProgress`, `InReview`, `Done`), optional assignee, estimate, and sprint foreign key. `ProjectEntity` and `WorkItem` have EF Core-required/max-length properties for their names/titles.

The name `ProjectEntity` avoids confusion with MSBuild project concepts. `WorkItem` avoids collision with `System.Threading.Tasks.Task`, which is widely used by the async code.

### Persistence and boundaries

EF Core uses SQL Server/LocalDB in development. `DevTrackDbContext` holds product domain data. `DevTrackIdentityDbContext` holds ASP.NET Core Identity users, roles and credential data. Keeping Identity separate is a bounded-context decision: identity can evolve and be secured independently from project-management data.

A generic `Repository<T>` encapsulates basic EF CRUD and `SaveChangesAsync`; specialised repositories add the specific eager-loading queries the UI needs (`Include` for Sprint/Project navigation properties). `DbContext` remains scoped through dependency injection; controllers do not instantiate it.

## Main flows to demonstrate

### 1. MVC work-item flow and a real business rule

Start with the running Web application: view projects/sprints/work items, then try creating a work item in an active sprint. Explain this request path:

`WorkItemsController.Create` → `IWorkItemService.CreateAsync` → `ISprintRepository.GetByIdAsync` → rule validation → `IWorkItemRepository.AddAsync` → `SaveChangesAsync`.

The key rule lives in `WorkItemService`: a work item cannot be added or moved into a missing or already-ended sprint. The rule is checked in both create and update operations so a user cannot bypass it by editing an existing item. The controller catches the business exception and turns it into a model-level validation error, then repopulates the sprint dropdown before rendering the form again.

### 2. Authentication and authorisation

The MVC host uses ASP.NET Core Identity and cookie authentication. A global MVC authorisation filter requires sign-in by default; selected Identity Razor Pages are explicitly anonymous. `Program.cs` seeds `Admin` and `TeamMember` roles. The `AdminOnly` policy protects delete actions, while signed-in users can view, create and edit.

The API host uses the same Identity store but authenticates clients with bearer JWTs. Login checks the password through `UserManager`, adds identity and role claims, and issues a one-hour signed JWT. JWT validation checks issuer, audience, signing key, lifetime, and has a one-minute clock skew. API controllers use `[Authorize]`; registration/login are explicitly `[AllowAnonymous]`.

### 3. Local AI planning is advisory, structured, and reviewed

The optional AI planner calls a local Ollama model through a typed `HttpClient`. It gives the model a restrictive system instruction and supplies a JSON schema requiring a summary, assumptions, and 3–7 subtasks with a 1–40 hour estimate. The implementation deserialises the response and validates the structure again in application code.

The important safety/product decision: suggestions are not silently written to the database. The user sees them, can edit/select them, and only selected items are created as `Backlog` work items. The service handles unavailable models, timeouts, and malformed output as useful, non-sensitive failures. Input is treated as untrusted data, not executable instructions.

## Best 15-minute walkthrough sequence

1. **0:00–1:00 — Product and problem.** “DevTrack is a small-team sprint tracker. I chose it because it is simple enough to demo end to end but rich enough to show realistic engineering decisions.”
2. **1:00–3:00 — Use the app.** Show a project, sprint, and work-item list; create or edit a work item. Avoid relying on a live AI response as the core demo.
3. **3:00–5:00 — Architecture.** Show the solution folders and explain dependency direction. Show Core entities/contracts, then `WorkItemService`, then Infrastructure repositories, then the MVC controller.
4. **5:00–7:00 — Follow one rule.** Open `WorkItemService.CreateAsync` and `UpdateAsync`. State why this rule is in the application layer instead of a controller: it is reused by MVC and AI-created work items, and it is independently testable.
5. **7:00–9:00 — Security.** Open Web and API `Program.cs`, then `AuthController`. Explain separate authentication mechanisms for browser/UI and API clients, roles/policy protection, and secrets being configuration rather than source code.
6. **9:00–11:00 — AI integration.** Open `OllamaTaskPlanningService` and `AiPlannerController`. Emphasise typed HTTP client, cancellation/timeouts, schema plus server-side validation, explicit human approval, and failure handling.
7. **11:00–13:00 — Tests.** Open `WorkItemServiceTests` and explain fakes prove both happy path and no persistence after invalid-sprint errors. Open the Ollama test to show an HTTP boundary test using a stub handler rather than a live model.
8. **13:00–14:00 — Delivery.** Show GitHub Actions: restore, Release build, tests with coverage, publish artifact, and deploy only after validation succeeds on non-PR runs.
9. **14:00–15:00 — Reflection.** State what you would improve next and invite questions.

## Exact files worth opening

1. `README.md` — concise feature/architecture overview and setup story.
2. `src/DevTrack.Core/Entities/ProjectEntity.cs`, `Sprint.cs`, `WorkItem.cs` — model and relationships.
3. `src/DevTrack.Core/Interfaces/IWorkItemRepository.cs` and `ITaskPlanningService.cs` — inward-facing contracts.
4. `src/DevTrack.Application/Services/WorkItemService.cs` — central business rule.
5. `src/DevTrack.Infrastructure/Data/DevTrackDbContext.cs` and `WorkItemRepository.cs` — EF mapping and eager-loading implementation.
6. `src/DevTrack.Web/Program.cs` and `Controllers/WorkItemsController.cs` — MVC composition, default authentication, policy use, and validation-to-UI translation.
7. `src/DevTrack.Api/Program.cs` and `Controllers/AuthController.cs` — JWT composition and token creation.
8. `src/DevTrack.Infrastructure/AI/OllamaTaskPlanningService.cs` and `src/DevTrack.Web/Controllers/AiPlannerController.cs` — external integration and approval workflow.
9. `tests/DevTrack.Tests/WorkItemServiceTests.cs` and `OllamaTaskPlanningServiceTests.cs` — test strategy.
10. `.github/workflows/ci-cd.yml` — quality gate and Azure App Service deployment.

## Questions to rehearse, with honest answer outlines

**Why not put the “closed sprint” rule in the controller?**  Controllers are delivery-specific. Putting it in `WorkItemService` means MVC and the AI creation flow receive the same enforcement, and it can be tested without HTTP, Razor, or EF Core.

**Why have a repository if EF Core already has DbContext?**  Here it makes the application layer depend on domain contracts and lets unit tests use fakes. It is intentionally thin; I would avoid adding abstractions that merely duplicate EF queries. For more complex work, I would consider explicit application/query handlers rather than expanding a generic repository indefinitely.

**Why separate the Identity and domain databases?**  Identity has a different security and lifecycle boundary. Separation reduces coupling and lets us independently manage identity schema and access. It adds operational overhead: migrations and backups must cover both stores, and cross-database transactions need deliberate design.

**How is a browser user different from an API client?**  The browser host uses Identity cookies; programmatic API clients log in and present short-lived bearer JWTs. Both share the same users/roles, while their transport and anti-forgery concerns differ.

**What prevents AI output from corrupting data?**  It is only a suggestion. The response is constrained with a schema, revalidated after deserialisation, shown to the user, and persisted only after selection. Selected items still enter the normal `WorkItemService` path, so the sprint rule still applies.

**How do you handle a failing integration?**  The typed `HttpClient` has a configured timeout; cancellation propagates. The UI/API catch connection, timeout, and invalid-response failures, log technical details, and return a safe, actionable message. The next production step would be observability, resilience policies, and a hosted/managed model endpoint.

**How would this relate to Visco’s Azure integration platform?**  The transferable thinking is boundary design: contracts at the core, adapters at the edge, explicit identity and least-privilege policies, predictable failure behaviour, automated delivery, and human-safe review for generated output. In an Azure integration platform I would apply those principles with managed identities/Key Vault, API Management, messaging where async decoupling is needed, Application Insights, and deployment slots—chosen based on actual throughput, reliability, and integration requirements rather than added by default.

**What tests matter most here?**  Unit tests cover business behaviour and persistence side-effects in `WorkItemService`; the AI test verifies request routing and structured response mapping without depending on a local model. Next I would add integration tests for EF/identity/API authorisation and end-to-end smoke tests in CI.

## Trade-offs and improvements to acknowledge proactively

- The product is a learning project, not yet a fully production-hardened SaaS. Be direct about that.
- `DateTime.Now` in the sprint rule makes boundary-time tests and time zones less explicit. Introduce an injected `TimeProvider`, define UTC/date semantics, and add boundary tests.
- Domain entities are used directly as MVC binding models. Introduce dedicated request/view models to reduce over-posting risk and make API/UI contracts more intentional.
- The AI planner’s selected-item loop saves each item independently. For all-or-nothing bulk creation, add a batch application service operation and a single transaction.
- Repository methods currently return materialised entity lists without paging/filtering. Add query projections, pagination, and appropriate indexes as data grows.
- The application has role-based access but no demonstrated ownership/tenant scoping; every authenticated user can access the shared domain data. Add user/organisation ownership and resource-based authorisation before multi-tenant use.
- The local Ollama endpoint is appropriate for cost-controlled development and privacy experimentation, but it is not a cloud deployment design. A deployed version needs a separately hosted model provider, robust secret management, monitoring, rate limits, and an availability/cost strategy.
- The Web project’s email sender logs messages rather than delivering them. Use a real provider and secure token/email observability for production.
- The API’s JWT signing key must live in secret configuration (for example Key Vault in Azure), be rotated deliberately, and never be included in a chat upload or repository.
- CI validates and deploys the Web host. Extend the pipeline with infrastructure/environment configuration, API deployment, migrations as a controlled release step, health checks, and post-deploy smoke tests.

## A credible engineering story

“One meaningful design challenge was keeping an AI-enhanced feature useful without allowing it to become an uncontrolled write path. I wanted it to reduce planning friction, but a model can be unavailable or return output that is incomplete, unreasonable, or not aligned to the team’s needs. I isolated the integration behind `ITaskPlanningService`, used a typed HTTP client with a constrained JSON shape, validated the result again in code, and made it a review-and-select workflow. Persisting selected suggestions still goes through the existing work-item service, so the same sprint lifecycle rule applies. The result is that the AI adds assistance, but the user and existing domain rules remain in control. If I took it to production, I would add telemetry, resilience policies, a managed model endpoint, and stronger auditability.”

Only call this a challenge you personally faced if it accurately reflects your experience. Keep the language in your own voice and be ready to discuss the actual decisions and code.

## Setup and demo safety checklist

- Run `dotnet restore`, `dotnet build DevTrack.slnx`, and `dotnet test DevTrack.slnx` before the interview.
- Start the MVC app and log into a prepared non-sensitive demo account. Confirm the database has a project, active sprint, and a few work items.
- Do not depend on the AI planner for the critical path. If you show it, pre-check that Ollama/model is running; otherwise explain the integration with its test and source code.
- Have the solution and the files listed above open in tabs. Keep the walkthrough at architecture and a single end-to-end feature rather than clicking through every CRUD page.
- Never display application settings, secrets, publish profiles, tokens, database files, browser password managers, or real personal data.
- Practice the first minute and final reflection aloud. It is better to explain one flow deeply than to claim familiarity with every file.

## Useful commands

```powershell
dotnet restore
dotnet build DevTrack.slnx
dotnet test DevTrack.slnx
dotnet run --project src/DevTrack.Web
```

