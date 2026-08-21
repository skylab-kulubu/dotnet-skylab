# Contributing to SKY LAB Forms API

Thanks for your interest in contributing! This document covers the project architecture, development setup, and guidelines for extending the Forms service while preserving Clean Architecture boundaries.

---

## Table of Contents

- [Development Setup](#development-setup)
- [Project Structure](#project-structure)
- [Architecture](#architecture)
- [Adding a Forms Feature](#adding-a-forms-feature)
- [Code Conventions](#code-conventions)
- [How to Contribute](#how-to-contribute)
- [Commit Convention](#commit-convention)

---

## Development Setup

### Prerequisites

- .NET 9.0 SDK
- PostgreSQL
- Redis
- An IDE (Visual Studio, Rider, or VS Code + C# Dev Kit)

### First-Time Setup

```bash
# Clone the repository
git clone <repo-url>
cd forms-backend

# Create the local Compose environment file
cp .env.example .env

# Restore, build, and run
dotnet restore src/Forms.sln
dotnet build src/Forms.sln
dotnet run --project src/Forms.Api
```

PowerShell equivalent:

```powershell
Copy-Item .env.example .env
dotnet restore src/Forms.sln
dotnet build src/Forms.sln
dotnet run --project src/Forms.Api
```

The `.env` file and `appsettings.Development.json` are ignored. Never commit secrets, credentials, or service-client tokens.

---

## Project Structure

```text
src/
├── Forms.Api/                             # Endpoints, middleware, Swagger, composition root
├── Forms.Application/                     # Use cases, contracts, ports, validation
│   ├── Abstractions/                      # External-service and persistence ports
│   ├── Contracts/                         # API-independent request/response models
│   ├── Services/                          # Forms use-case implementations
│   └── DependencyInjection.cs             # Application service registration
├── Forms.Domain/                          # Entities, enums, domain models and rules
├── Forms.Infrastructure/                  # Technical adapters and persistence
│   ├── Auth/                              # Current-user, external-user, Keycloak adapters
│   ├── Caching/                           # Redis implementation
│   ├── Exports/                           # ClosedXML implementation
│   ├── Mail/                              # SkyMail client, queue, and background workers
│   ├── Migrations/                        # EF Core migrations
│   ├── Storage/                           # DbContext, configurations, repositories
│   └── DependencyInjection.cs             # Infrastructure registration
└── Forms.sln
```

---

## Architecture

### Layers

| Layer | Responsibility | Allowed project dependencies |
|-------|----------------|--------------------------------|
| **Domain** | Entities, enums, domain models, domain behavior | None |
| **Application** | Use cases, contracts, validation, ports, result types | Domain |
| **Infrastructure** | PostgreSQL, EF Core, Redis, HTTP, Keycloak, mail, Excel, workers | Application, Domain |
| **API** | Minimal API endpoints and composition root | Application, Infrastructure |

The dependency flow is inward:

```text
Forms.Api ───────────────┐
                        v
Forms.Infrastructure -> Forms.Application -> Forms.Domain
```

### Boundary Rules

1. **Domain must remain independent.** Do not reference Application, Infrastructure, ASP.NET Core, EF Core, Redis, or HTTP packages.
2. **Application must not reference Infrastructure or API.** Define external capabilities as interfaces under `Forms.Application/Abstractions`.
3. **Infrastructure implements Application ports.** Database, cache, mail, Excel, identity, and hosted-worker code belong here.
4. **API must stay thin.** Bind HTTP input, resolve the current user, call an Application service, and map the result.
5. **Do not recreate generic Shared projects.** If a type exists only for Forms, place it in the owning Forms layer.
6. **Do not create a separate module for a Forms capability.** For example, Excel generation is an Infrastructure adapter behind `IExcelService`.

### Key Patterns

- **Clean Architecture** with inward project references
- **DDD-oriented Forms bounded context**
- **Repository Pattern** and **Unit of Work**
- **Result Pattern** through `ServiceResult<T>`
- **Minimal APIs** with `MapGroup`
- **JSONB Storage** for dynamic forms and responses
- **Dependency Inversion** for Redis, external users, Keycloak, SkyMail, and Excel

---

## Adding a Forms Feature

New work should extend the existing Forms bounded context rather than create a new service module inside this repository.

### Step 1 - Identify the Owning Layer

Use the following decision guide:

- **Business state or invariant:** Domain
- **Use case or orchestration:** Application
- **External system or technical implementation:** Infrastructure
- **HTTP transport:** API

Example feature: adding an expiration date to forms.

### Step 2 - Update the Domain

Add the business state and behavior to the relevant entity when the rule belongs to the model:

```csharp
// src/Forms.Domain/Entities/Form.cs
public DateTime? ExpiresAt { get; set; }

public bool IsExpired(DateTime utcNow) => ExpiresAt.HasValue && ExpiresAt <= utcNow;
```

Keep this code free from persistence and HTTP concerns.

### Step 3 - Add or Update Application Contracts

Update request and response contracts under the matching feature folder:

```csharp
// src/Forms.Application/Contracts/Forms/FormUpsertRequest.cs
public record FormUpsertRequest(
    string Title,
    string? Description,
    DateTime? ExpiresAt
);
```

### Step 4 - Implement the Use Case

Application services should coordinate domain behavior and abstractions:

```csharp
public async Task<ServiceResult<FormContract>> UpdateFormAsync(
    Guid formId,
    FormUpsertRequest request,
    Guid userId,
    CancellationToken cancellationToken = default)
{
    // Load through an Application repository abstraction.
    // Enforce authorization and domain rules.
    // Persist through IFormsUnitOfWork.
}
```

All I/O methods should accept and forward `CancellationToken`.

### Step 5 - Add an Application Port When Needed

If the use case needs a new external capability, define the interface in Application:

```csharp
// src/Forms.Application/Abstractions/IFormDocumentService.cs
public interface IFormDocumentService
{
    Task<byte[]> GenerateAsync(Guid formId, CancellationToken cancellationToken = default);
}
```

Do not put an HTTP client, SDK, or vendor type in the Application contract.

### Step 6 - Implement Infrastructure

Implement the port under the relevant Infrastructure feature folder:

```csharp
// src/Forms.Infrastructure/Documents/FormDocumentService.cs
public sealed class FormDocumentService : IFormDocumentService
{
    public Task<byte[]> GenerateAsync(Guid formId, CancellationToken cancellationToken = default)
    {
        // Technical implementation
    }
}
```

Register the implementation in `src/Forms.Infrastructure/DependencyInjection.cs`.

### Step 7 - Update Persistence

For entity changes:

1. Update the entity configuration under `Forms.Infrastructure/Storage/Configurations`.
2. Create a migration:

```bash
dotnet ef migrations add AddFormExpiration \
  --project src/Forms.Infrastructure \
  --startup-project src/Forms.Api
```

3. Review the migration and snapshot before committing.

Migrations are applied automatically during application startup.

### Step 8 - Define or Update API Endpoints

Keep endpoints transport-focused:

```csharp
group.MapPut("/{id:guid}", async (
    Guid id,
    FormUpsertRequest request,
    IFormService service,
    ICurrentUserService currentUser,
    CancellationToken cancellationToken) =>
{
    var userId = await currentUser.GetUserIdAsync(cancellationToken);
    if (userId is null)
        return ServiceStatus.Unauthorized.ToApiResult();

    var result = await service.UpdateFormAsync(id, request, userId.Value, cancellationToken);
    return result.ToApiResult();
});
```

Do not place business rules or direct DbContext access in endpoint files.

### Step 9 - Register Services

- Register use-case services in `Forms.Application/DependencyInjection.cs`.
- Register adapters, repositories, clients, and workers in `Forms.Infrastructure/DependencyInjection.cs`.
- Avoid growing `Forms.Api/Program.cs` with individual service registrations.

### Step 10 - Validate

```bash
dotnet restore src/Forms.sln
dotnet build src/Forms.sln -c Release
dotnet list src/Forms.sln package --vulnerable --include-transitive
```

If Docker configuration changed:

```powershell
docker compose config --quiet
```

### Checklist

- [ ] The owning architectural layer is correct
- [ ] Domain remains framework-independent
- [ ] Application references only Domain
- [ ] External dependencies are represented by Application ports
- [ ] Infrastructure implementations are registered centrally
- [ ] Endpoint code remains thin
- [ ] Cancellation tokens flow through I/O operations
- [ ] Migration and model snapshot are reviewed when persistence changes
- [ ] Release build succeeds without warnings
- [ ] Package vulnerability scan is clean
- [ ] Documentation is updated when behavior or configuration changes

---

## Code Conventions

### Naming

| Element | Format | Example |
|---------|--------|---------|
| Entity | PascalCase, singular | `Form`, `FormResponse` |
| Request contract | PascalCase + `Request` suffix | `FormUpsertRequest` |
| Response/read contract | PascalCase + `Contract` suffix | `FormContract` |
| Service interface | `I` + PascalCase + `Service` | `IFormService` |
| Service implementation | PascalCase + `Service` | `FormService` |
| Endpoint groups | kebab-case, plural | `/api/admin/forms` |

### Key Rules

- Application service methods return `ServiceResult<T>`.
- Endpoints convert results via `.ToApiResult()`.
- Soft delete is preferred where historical Forms data must be retained.
- JSONB is used for flexible form and response schema data.
- Entity configurations belong in separate `IEntityTypeConfiguration<T>` classes.
- Authorization rules belong in Application services, using `ICurrentUserService` where required.
- Admin endpoints live under `/api/admin/forms`; public endpoints live under `/api/forms`.
- Never swallow `OperationCanceledException`.
- Do not expose Infrastructure response models through Application contracts.
- Prefer feature-specific names over generic helper or shared abstractions.

---

## How to Contribute

1. Fork the repository.
2. Create your feature branch (`git checkout -b feat/amazing-feature`).
3. Implement the change within the architectural boundaries.
4. Run the Release build and package vulnerability scan.
5. Commit your changes (`git commit -m "feat: add amazing feature"`).
6. Push the branch and open a Pull Request.

---

## Commit Convention

This project follows conventional commits:

| Prefix | Usage |
|--------|-------|
| `feat:` | New features |
| `fix:` | Bug fixes |
| `refactor:` | Code refactoring |
| `docs:` | Documentation |
| `test:` | Tests |
| `build:` | Build or dependency changes |
| `chore:` | Maintenance tasks |