# DevTrack interview architecture walkthrough

## A 3–5 minute explanation

“DevTrack is a small-team sprint tracker. It lets authenticated users manage projects, sprints, and work items through an ASP.NET Core MVC/Razor UI, and it also exposes a JWT-protected HTTP API for programmatic clients. The main domain is intentionally simple: a project has many sprints, and a sprint has many work items. A work item has a numeric status, optional text assignee, estimate, and required sprint reference ([WorkItem.cs](../src/DevTrack.Core/Entities/WorkItem.cs#L14)).

The solution has five projects. Core contains the entities plus interfaces, with no project dependencies. Application currently contains `WorkItemService`, where the key business rule lives: you cannot create or move a work item to a missing or already-ended sprint. Infrastructure implements the repository interfaces using EF Core, contains two DbContexts and migrations, and adapts a local Ollama AI service. Web and API are the delivery hosts that wire those components together through dependency injection ([WorkItemService.cs](../src/DevTrack.Application/Services/WorkItemService.cs#L33), [Web Program.cs](../src/DevTrack.Web/Program.cs#L47)).

For persistence, EF Core targets SQL Server. There are two separately configured databases: `DevTrackDb` for product data and `DevTrackIdentityDb` for ASP.NET Core Identity. The schema has mandatory cascade relationships from project to sprint and sprint to work item. That separation keeps Identity’s tables distinct from the product model, although it means migrations and operations need to cover two databases ([domain migration](../src/DevTrack.Infrastructure/Migrations/20260715205141_InitialCreate.cs#L29)).

Here is one request I would trace. When a browser user creates a work item, the MVC controller validates the request and calls `WorkItemService`. The service reads the sprint, rejects a missing or ended one, then stages and saves the work item through the repository. The MVC controller catches that business exception and renders it as a form error; the API maps the same exception to a validation-problem response. Keeping that rule in the application service means MVC, API, and selected AI suggestions take the same path rather than each controller reinventing it ([MVC controller](../src/DevTrack.Web/Controllers/WorkItemsController.cs#L47), [API controller](../src/DevTrack.Api/Controllers/WorkItemsController.cs#L25)).

Security differs by client. MVC uses Identity cookies and a global authorization filter. The API uses signed one-hour bearer JWTs after `UserManager` verifies the password. Both create `Admin` and `TeamMember` roles; delete is protected by the `AdminOnly` policy, while authenticated users can read/create/edit. There is not yet ownership or tenant scoping, so it is appropriate for a shared team tracker rather than a multi-tenant SaaS ([API Program.cs](../src/DevTrack.Api/Program.cs#L33)).

Finally, the optional AI planner calls local Ollama through a typed HTTP client. It asks for structured suggestions, validates the returned structure, displays the suggestions for review, and only persists selections through `WorkItemService`. That makes the model advisory rather than an uncontrolled write path. Tests focus on the core rule with fake repositories and the Ollama HTTP boundary with a stub handler. The repository has a GitHub Actions pipeline that builds, tests, publishes, and deploys the Web host to Azure App Service; the API and migrations are not deployed by that workflow ([ci-cd.yml](../.github/workflows/ci-cd.yml#L13)).”

## Likely questions and code-grounded answers

### Why have a service layer if there are repositories?

`WorkItemService` owns orchestration across two repositories and the closed-sprint rule. Controllers remain delivery-specific, and tests can fake the contracts. The application layer is currently narrow: project/sprint CRUD still coordinates directly in controllers. That is an accurate description of the current boundary, not a claim that all domain logic is centralized ([WorkItemService.cs](../src/DevTrack.Application/Services/WorkItemService.cs#L10)).

### Why two databases?

Confirmed fact: the hosts configure separate contexts/connection strings, one for product records and one for Identity tables ([Web Program.cs](../src/DevTrack.Web/Program.cs#L16)). **Inference:** this separates credential/Identity schema lifecycle from the domain schema. The cost is two migrations and no cross-database transaction in the codebase.

### How are relationships and deletion protected?

SQL Server enforces mandatory FK relationships. It cascades project deletion to sprints and sprint deletion to work items. The migrations also index both FK columns ([InitialCreate.cs](../src/DevTrack.Infrastructure/Migrations/20260715205141_InitialCreate.cs#L43)). Application authorization separately limits delete operations to `Admin`; database cascade still applies once an authorized deletion is made.

### What prevents a work item being put into a closed sprint?

`WorkItemService.CreateAsync` and `UpdateAsync` read the selected sprint, reject null, then compare `EndDate` with `DateTime.Now` before calling `SaveChangesAsync` ([WorkItemService.cs](../src/DevTrack.Application/Services/WorkItemService.cs#L33)). This is application-only; direct SQL could bypass it. It also uses server-local time, so exact time-zone semantics need improvement.

### How does browser security differ from API security?

The Web host uses Identity cookie authentication, Razor/anti-forgery forms, and an authenticated-by-default MVC filter. The API allows only register/login anonymously, validates bearer JWTs, and returns JSON/Problem Details ([Web Program.cs](../src/DevTrack.Web/Program.cs#L27), [AuthController.cs](../src/DevTrack.Api/Controllers/AuthController.cs#L13)). Both rely on the same Identity role store.

### What does the AI feature do to protect data quality?

It constrains requested JSON, deserializes it, and checks the summary, count, title, description, and estimate bounds before returning suggestions ([OllamaTaskPlanningService.cs](../src/DevTrack.Infrastructure/AI/OllamaTaskPlanningService.cs#L12), [OllamaTaskPlanningService.cs](../src/DevTrack.Infrastructure/AI/OllamaTaskPlanningService.cs#L89)). Users review/select suggestions; persistence then goes through the normal work-item service. It is not a guarantee that text is correct—only that its structure meets these checks.

### What is tested, and what is not?

Focused xUnit tests prove the service persists for active sprints and does not persist for missing/ended sprints. A stub handler proves the Ollama request path and response mapping without a live model ([WorkItemServiceTests.cs](../tests/DevTrack.Tests/WorkItemServiceTests.cs#L9), [OllamaTaskPlanningServiceTests.cs](../tests/DevTrack.Tests/OllamaTaskPlanningServiceTests.cs#L11)). There are no checked-in integration tests for migrations, Identity/JWT authorization, API endpoints, or browser flows.

### What really deploys today?

Repository evidence shows GitHub Actions deploying the published MVC Web project to Azure App Service `devtrack-web` after build/test on non-PR runs. It does not show API deployment, migration execution, infrastructure-as-code, health checks, or a reachable Ollama deployment ([ci-cd.yml](../.github/workflows/ci-cd.yml#L44)). Do not claim those are implemented.

## Suggested improvements (not current implementation)

- Move connection strings and the JWT signing key entirely out of source configuration; rotate keys and use managed secret storage.
- Add user/organization ownership and resource-based authorization before treating the app as multi-tenant.
- Use dedicated MVC request/view models for all CRUD routes, consistently validate sprint dates/estimates/statuses, and add database check constraints where rules must survive direct writes.
- Introduce `TimeProvider` and explicit UTC/date semantics for sprint closure.
- Add a batch work-item application operation with one transaction for selected AI suggestions.
- Add paged/project-filtered query projections and evaluate indexes from real workload data.
- Add API/EF/Identity integration tests plus deployment smoke/health checks; explicitly deploy/migrate API and both databases.
- For AI, add a production provider strategy, secret management, resilience/rate limits, telemetry, and appropriate auditability.
- Replace the logging-only email sender with a real provider and establish safe email/token logging practices.

## Claims to phrase carefully

- Say “the repository has a Web deployment workflow,” not “the whole system is deployed to Azure.”
- Say “the code separates domain and Identity contexts”; calling it a deliberate bounded-context decision is an **inference** unless that reflects your own decision.
- Say “SQL Server/LocalDB is configured in checked-in settings”; the live database/schema and production database host are **unknown**.
- Do not describe the checked-in JWT key as production secret management. It is visible in configuration and must be changed for production.
- Do not say MVC has the same request-DTO validation as the API. It binds domain entities, while API DTOs carry the stronger annotations.
