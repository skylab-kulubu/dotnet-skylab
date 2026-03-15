<div align="center">
  <img src=".github/skylab.svg" alt="SKY LAB Logo" width="80" />
  <h1>SKY LAB .NET Backend</h1>
  <p>
    A modular .NET backend powering the services of<br/>
    <strong>Yıldız Technical University - SKY LAB</strong>
  </p>
  <p>
    <img src="https://img.shields.io/badge/.NET-9.0-512BD4?style=for-the-badge&logo=dotnet" alt=".NET" />
    <img src="https://img.shields.io/badge/PostgreSQL-16-4169E1?style=for-the-badge&logo=postgresql&logoColor=white" alt="PostgreSQL" />
    <img src="https://img.shields.io/badge/Docker-Ready-2496ED?style=for-the-badge&logo=docker&logoColor=white" alt="Docker" />
  </p>
</div>

<br/>

> Modular monolith backend with Clean Architecture - currently serving Forms and Export modules, designed to grow with new service modules over time.

---

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Runtime | .NET 9.0 / C# 12 |
| Database | PostgreSQL (EF Core + Npgsql) |
| API | ASP.NET Core Minimal APIs |
| Service Discovery | Steeltoe Eureka |
| Excel Export | ClosedXML |
| Documentation | Swagger / OpenAPI |
| Container | Docker (multi-stage build) |

## Architecture

The project follows a **modular monolith** architecture, designed with Clean Architecture and DDD principles. Each module has its own Domain, Application, and Infrastructure layers.

```
src/
├── API/
│   └── Skylab.Api/                        # Main API entry point, endpoint definitions, DI config
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

### Layer Responsibilities

- **Domain** - Entities, enums, and domain rules. No external dependencies.
- **Application** - Service interfaces/implementations, DTOs, validators, and business logic orchestration.
- **Infrastructure** - EF Core DbContext, entity configurations (including JSONB support), migrations.
- **API** - Minimal API endpoints, dependency injection, middleware configuration.

## Modules

### Forms Module

Dynamic form creation and response management module.

**Core Features:**

- Form CRUD operations
- JSONB-based flexible form schema support
- Response collection and management
- Linked forms (multi-step workflows)
- Collaborator management with roles (Owner, Editor, Viewer)
- Manual review workflow (Pending → Approved / Declined)
- Response archiving
- Form metrics and analytics
- Reusable component groups
- Anonymous response support
- Single/multiple response control

**Database Models:**

| Table | Description |
|-------|------------|
| `Forms` | Form definitions, schema (JSONB), status, settings |
| `Responses` | User responses, review info, archive status |
| `FormCollaborators` | Collaborator roles (composite key: UserId + FormId) |
| `ComponentGroups` | Reusable form component templates |

### Exports Module

General-purpose Excel file generation service.

**Features:**

- Generates XLSX files from header + row data
- Custom sheet name support
- Auto-adjusting column widths
- Returns byte array output (download-ready)

## API Endpoints

### Form - Public

| Method | Endpoint | Description |
|--------|----------|------------|
| `GET` | `/api/forms/{id}` | Get form for display |
| `GET` | `/api/forms/{id}/meta` | Get form metadata |
| `POST` | `/api/forms/responses` | Submit a response |

### Form - Admin

| Method | Endpoint | Description |
|--------|----------|------------|
| `GET` | `/api/admin/forms/` | List forms (paginated, searchable) |
| `POST` | `/api/admin/forms/` | Create new form |
| `GET` | `/api/admin/forms/{id}` | Get form details |
| `PUT` | `/api/admin/forms/{id}` | Update form |
| `DELETE` | `/api/admin/forms/{id}` | Delete form (soft delete) |
| `GET` | `/api/admin/forms/{id}/info` | Get form info |
| `GET` | `/api/admin/forms/{id}/linkable-forms` | Get linkable forms |
| `GET` | `/api/admin/forms/{id}/responses` | List responses |
| `GET` | `/api/admin/forms/{id}/responses/export` | Export responses to Excel |
| `GET` | `/api/admin/forms/{id}/metrics` | Get form metrics |
| `GET` | `/api/admin/forms/responses/{id}` | Get single response |
| `PATCH` | `/api/admin/forms/responses/{id}/status` | Update response status |
| `POST` | `/api/admin/forms/responses/{id}/archive` | Archive response |
| `GET` | `/api/admin/forms/metrics` | Get service-wide metrics |

### Component Groups - Admin

| Method | Endpoint | Description |
|--------|----------|------------|
| `GET` | `/api/admin/forms/component-groups` | List groups |
| `GET` | `/api/admin/forms/component-groups/{id}` | Get group details |
| `POST` | `/api/admin/forms/component-groups` | Create group |
| `PUT` | `/api/admin/forms/component-groups/{id}` | Update group |
| `DELETE` | `/api/admin/forms/component-groups/{id}` | Delete group |

### Export - Internal

| Method | Endpoint | Description |
|--------|----------|------------|
| `POST` | `/internal/api/exports/excel` | Generate Excel file |

## Authentication & Authorization

- **Authentication:** Remote verification via external `super-skylab` service using Bearer token forwarding. The `Authorization` header is forwarded to `/internal/api/users/authenticated-user`.
- **Authorization:** Role-based access control enforced at the service layer:
  - **Owner** - Full control, collaborator management
  - **Editor** - Edit forms, view responses
  - **Viewer** - Read-only access

## Getting Started

### Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [PostgreSQL](https://www.postgresql.org/download/)

### Running Locally

```bash
# Restore dependencies
dotnet restore src/Skylab.sln

# Run the application
dotnet run --project src/API/Skylab.Api

# Database migrations run automatically on startup
```

Swagger UI: `http://localhost:8080/swagger`

### Docker

```bash
# Build image
docker build -f src/Dockerfile -t skylab-forms-api .

# Run container
docker run -p 8080:8080 \
  -e CONNECTION_STRING="Host=db;Port=5432;Database=forms_db;Username=admin;Password=skylab" \
  -e ALLOWED_ORIGIN="http://localhost:3000" \
  skylab-forms-api
```

## Configuration

| Variable | Description | Required |
|----------|------------|----------|
| `CONNECTION_STRING` | PostgreSQL connection string | Yes |
| `ALLOWED_ORIGIN` | CORS allowed origin | Yes |

Database connection is configured with an automatic retry strategy (5 retries, max 10s delay).
