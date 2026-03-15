# Contributing to SKY LAB .NET Backend

Thanks for your interest in contributing! This document covers the project architecture, development setup, and guidelines for adding new service modules.

---

## Table of Contents

- [Development Setup](#development-setup)
- [Project Structure](#project-structure)
- [Architecture](#architecture)
- [Adding a New Service Module](#adding-a-new-service-module)
- [Code Conventions](#code-conventions)
- [How to Contribute](#how-to-contribute)
- [Commit Convention](#commit-convention)

---

## Development Setup

### Prerequisites

- .NET 9.0 SDK
- PostgreSQL
- An IDE (Visual Studio, Rider, or VS Code + C# Dev Kit)

### First-Time Setup

```bash
# Clone the repo
git clone <repo-url>
cd forms-backend

# Create .env file
cp src/API/Skylab.Api/.env.example src/API/Skylab.Api/.env
# Edit .env with your local values

# Restore dependencies and run
dotnet restore src/Skylab.sln
dotnet run --project src/API/Skylab.Api
```

---

## Project Structure

```
src/
├── API/
│   └── Skylab.Api/                        # Entry point - DI, endpoints, middleware
├── Modules/
│   ├── Forms/                             # Form management module
│   │   ├── Forms.Domain/                  #   Entities, enums, domain models
│   │   ├── Forms.Application/             #   Services, DTOs, business logic
│   │   └── Forms.Infrastructure/          #   DbContext, entity configs, migrations
│   └── Exports/                           # Excel export module
│       └── Exports.Application/           #   Excel file generation service
└── Shared/                                # Shared libraries across modules
    ├── Skylab.Shared.Domain/              #   Common domain types
    └── Skylab.Shared.Application/         #   Common service interfaces, result pattern
```

---

## Architecture

### Layers

| Layer | Responsibility | Dependencies |
|-------|---------------|-------------|
| **Domain** | Entities, enums, domain rules | None (pure C#) |
| **Application** | Service interfaces/implementations, DTOs, business logic | Domain, Shared |
| **Infrastructure** | DbContext, entity configurations, migrations | Domain, Application |
| **API** | Endpoint definitions, DI configuration | All layers |

> **Rule:** The dependency flow is always inward: API → Infrastructure → Application → Domain. The Domain layer must never depend on anything.

### Existing Modules

| Module | Layers | Description |
|--------|--------|------------|
| **Forms** | Domain + Application + Infrastructure | Form CRUD, response management, collaboration, metrics |
| **Exports** | Application only | General-purpose Excel export service |

> Not every module needs all layers. Simple services like Exports can work with just an Application layer.

### Key Patterns

- **Clean Architecture** with DDD principles per module
- **Repository Pattern** via EF Core DbContext
- **Result Pattern** - all service methods return `ServiceResult<T>`
- **Minimal APIs** with `MapGroup` for endpoint organization
- **JSONB Storage** for flexible schema data (forms, responses)

---

## Adding a New Service Module

Below is a step-by-step guide for adding a new module, demonstrated with a `Notifications` example.

### Step 1 - Create the Project Structure

Determine which layers you need based on the module's complexity:

- **Application only** - Simple services with no database needs (e.g., Exports)
- **Domain + Application + Infrastructure** - Modules with entities and database operations (e.g., Forms)

```bash
# Create module directory
mkdir -p src/Modules/Notifications

# Create the required projects
dotnet new classlib -n Notifications.Domain -o src/Modules/Notifications/Notifications.Domain -f net9.0
dotnet new classlib -n Notifications.Application -o src/Modules/Notifications/Notifications.Application -f net9.0
dotnet new classlib -n Notifications.Infrastructure -o src/Modules/Notifications/Notifications.Infrastructure -f net9.0
```

### Step 2 - Add to the Solution

```bash
dotnet sln src/Skylab.sln add \
  src/Modules/Notifications/Notifications.Domain/Notifications.Domain.csproj \
  src/Modules/Notifications/Notifications.Application/Notifications.Application.csproj \
  src/Modules/Notifications/Notifications.Infrastructure/Notifications.Infrastructure.csproj
```

### Step 3 - Set Up Project References

Establish the dependency chain:

```bash
# Application → Domain
dotnet add src/Modules/Notifications/Notifications.Application reference \
  src/Modules/Notifications/Notifications.Domain

# Application → Shared.Domain & Shared.Application
dotnet add src/Modules/Notifications/Notifications.Application reference \
  src/Shared/Skylab.Shared.Domain \
  src/Shared/Skylab.Shared.Application

# Infrastructure → Domain & Application
dotnet add src/Modules/Notifications/Notifications.Infrastructure reference \
  src/Modules/Notifications/Notifications.Domain \
  src/Modules/Notifications/Notifications.Application

# API → All layers
dotnet add src/API/Skylab.Api reference \
  src/Modules/Notifications/Notifications.Application \
  src/Modules/Notifications/Notifications.Infrastructure
```

### Step 4 - Domain Layer

Define your entities and enums:

```csharp
// src/Modules/Notifications/Notifications.Domain/Entities/Notification.cs
namespace Notifications.Domain.Entities;

public class Notification
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

### Step 5 - Application Layer

Create the service interface, implementation, and DTOs:

```csharp
// src/Modules/Notifications/Notifications.Application/Contracts/NotificationDto.cs
namespace Notifications.Application.Contracts;

public record NotificationDto(Guid Id, string Title, string Message, bool IsRead, DateTime CreatedAt);
```

```csharp
// src/Modules/Notifications/Notifications.Application/Services/INotificationService.cs
using Skylab.Shared.Application;
using Notifications.Application.Contracts;

namespace Notifications.Application.Services;

public interface INotificationService
{
    Task<ServiceResult<List<NotificationDto>>> GetUserNotificationsAsync();
    Task<ServiceResult<bool>> MarkAsReadAsync(Guid id);
}
```

```csharp
// src/Modules/Notifications/Notifications.Application/Services/NotificationService.cs
namespace Notifications.Application.Services;

public class NotificationService : INotificationService
{
    // Dependencies are injected via constructor
    // Business logic goes here
}
```

### Step 6 - Infrastructure Layer

Add DbContext configuration. If the module extends the existing DbContext:

```csharp
// Add a DbSet to the existing FormsDbContext (or create a new one)
public DbSet<Notification> Notifications { get; set; }
```

Entity configuration:

```csharp
// src/Modules/Notifications/Notifications.Infrastructure/Configurations/NotificationConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notifications.Domain.Entities;

namespace Notifications.Infrastructure.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Title).IsRequired().HasMaxLength(200);
        builder.HasIndex(n => n.UserId);
    }
}
```

### Step 7 - Define API Endpoints

Register the service and endpoints in `Program.cs`:

```csharp
// Service registration
builder.Services.AddScoped<INotificationService, NotificationService>();

// Endpoints
var notificationEndpoints = app.MapGroup("/api/notifications");
notificationEndpoints.MapGet("/", async (INotificationService service) =>
{
    var result = await service.GetUserNotificationsAsync();
    return result.ToApiResult();
});
```

### Step 8 - Create Migration

If new entities were added:

```bash
dotnet ef migrations add AddNotifications \
  --project src/Modules/Notifications/Notifications.Infrastructure \
  --startup-project src/API/Skylab.Api
```

Migrations run automatically on application startup.

### Checklist

Steps to complete when adding a new module:

- [ ] Module directory and projects created
- [ ] Added to the solution
- [ ] Project references set up (correct dependency chain)
- [ ] Domain entities defined (if needed)
- [ ] DTOs created
- [ ] Service interface and implementation written
- [ ] Infrastructure configurations added (if needed)
- [ ] Service registered in `Program.cs`
- [ ] Endpoints defined
- [ ] Migration created (if needed)

---

## Code Conventions

### Naming

| Element | Format | Example |
|---------|--------|---------|
| Entity | PascalCase, singular | `Form`, `Notification` |
| DTO | PascalCase + `Dto` suffix | `FormDto`, `NotificationDto` |
| Service interface | `I` + PascalCase + `Service` | `IFormService` |
| Service implementation | PascalCase + `Service` | `FormService` |
| Endpoint groups | kebab-case, plural | `/api/admin/forms`, `/api/notifications` |

### Key Rules

- Service methods return `ServiceResult<T>` from `Skylab.Shared.Application`
- Endpoints convert results via `.ToApiResult()` extension method
- Soft delete is preferred over hard delete
- JSONB columns for flexible schema data
- Entity configurations in separate files using `IEntityTypeConfiguration<T>`
- Authorization checks at the service layer via `ICurrentUserService`
- Admin endpoints under `/api/admin/`, internal endpoints under `/internal/api/`

---

## How to Contribute

1. Fork the repository
2. Create your feature branch (`git checkout -b feat/amazing-feature`)
3. Commit your changes (`git commit -m 'feat: add amazing feature'`)
4. Push to the branch (`git push origin feat/amazing-feature`)
5. Open a Pull Request

---

## Commit Convention

This project follows conventional commits:

| Prefix       | Usage              |
| ------------ | ------------------ |
| `feat:`      | New features       |
| `fix:`       | Bug fixes          |
| `refactor:`  | Code refactoring   |
| `docs:`      | Documentation      |
| `test:`      | Tests              |
