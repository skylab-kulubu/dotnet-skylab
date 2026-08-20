<div align="center">
  <img src=".github/skylab.svg" alt="SKY LAB Logo" width="80" />
  <h1>SKY LAB Forms API</h1>
  <p>
    The dedicated forms backend powering<br/>
    <strong>Yıldız Technical University - SKY LAB</strong>
  </p>
  <p>
    <img src="https://img.shields.io/badge/.NET-9.0-512BD4?style=for-the-badge&logo=dotnet" alt=".NET" />
    <img src="https://img.shields.io/badge/PostgreSQL-16-4169E1?style=for-the-badge&logo=postgresql&logoColor=white" alt="PostgreSQL" />
    <img src="https://img.shields.io/badge/Redis-7-DC382D?style=for-the-badge&logo=redis&logoColor=white" alt="Redis" />
    <img src="https://img.shields.io/badge/Docker-Ready-2496ED?style=for-the-badge&logo=docker&logoColor=white" alt="Docker" />
  </p>
</div>

<br/>

> A Forms-only backend organized as a single bounded context with strict Clean Architecture dependency rules.

---

## Tech Stack

| Layer | Technology |
|-------|------------|
| Runtime | .NET 9.0 / C# |
| Database | PostgreSQL (EF Core 9 + Npgsql) |
| Cache & Drafts | Redis |
| API | ASP.NET Core Minimal APIs |
| Service Discovery | Steeltoe Eureka |
| Service Authentication | Keycloak client credentials |
| Excel Export | ClosedXML |
| Documentation | Swagger / OpenAPI |
| Container | Docker (multi-stage build) |

## Architecture

The repository contains one service and one bounded context: **Forms**. The former Feedbacks module, generic Exports module, and Shared projects have been removed. Capabilities that belong to Forms now live in the appropriate Forms layer.

```text
src/
├── Forms.Domain/                          # Entities, enums, domain models and rules
├── Forms.Application/                     # Use cases, contracts, validators and ports
├── Forms.Infrastructure/                  # EF Core, Redis, HTTP clients, mail, Excel, workers
├── Forms.Api/                             # Minimal API endpoints and composition root
└── Forms.sln
```

### Dependency Direction

```text
Forms.Api ───────────────┐
                        v
Forms.Infrastructure -> Forms.Application -> Forms.Domain
```

### Layer Responsibilities

- **Domain** - Forms entities, enums, domain models, and domain behavior. It has no project or framework dependency.
- **Application** - Use-case services, repository/external-service abstractions, request and response contracts, validators, result types, and orchestration. It depends only on Domain.
- **Infrastructure** - EF Core persistence, PostgreSQL migrations, Redis, identity clients, SkyMail integration, Excel generation, and background workers. It implements Application ports.
- **API** - Minimal API endpoints, middleware, Swagger, CORS, service discovery, and dependency composition.

### Key Patterns

- **Clean Architecture** with inward-only project dependencies
- **DDD-oriented Forms bounded context**
- **Repository and Unit of Work abstractions**
- **Result Pattern** through `ServiceResult<T>`
- **Minimal APIs** with endpoint groups
- **JSONB storage** for flexible form and response schemas
- **Port and Adapter approach** for Redis, identity, mail, and Excel

## Forms Capabilities

Dynamic form creation and response management service.

**Core Features:**

- Form CRUD operations and soft deletion
- JSONB-based flexible form schema support
- Response collection and management
- Linked forms for multi-step workflows
- Collaborator management with Owner, Editor, and Viewer roles
- Manual review workflow (`Pending -> Approved / Declined`)
- Response archiving
- Form metrics and answer analytics
- Reusable component groups
- Anonymous response support
- Single or multiple response control
- Redis-backed form and response drafts
- Response and component-group sharing tokens
- XLSX response export
- Mail notifications and pending-response reminders

**Database Models:**

| Table | Description |
|-------|-------------|
| `Forms` | Form definitions, JSONB schema, status, and response settings |
| `Responses` | User responses, review information, archive state, and timing |
| `FormCollaborators` | Collaborator roles with a composite user/form key |
| `ComponentGroups` | Reusable form component templates |

## API Endpoints

### Forms - Public

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/forms/{id}` | Get a form for display |
| `GET` | `/api/forms/{id}/meta` | Get public form metadata |
| `POST` | `/api/forms/responses` | Submit a response |
| `POST` | `/api/forms/responses/draft` | Save an authenticated user's response draft |
| `GET` | `/api/forms/responses/draft/{formId}` | Get an authenticated user's response draft |
| `DELETE` | `/api/forms/responses/draft/{formId}` | Delete an authenticated user's response draft |
| `GET` | `/api/forms/component-groups/{id}/meta` | Get shared component-group metadata |
| `GET` | `/api/forms/responses/{id}/meta` | Get shared response metadata |

### Forms - Admin

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/admin/forms/` | List the current user's forms |
| `GET` | `/api/admin/forms/all` | List all forms for service administrators |
| `POST` | `/api/admin/forms/` | Create a form |
| `GET` | `/api/admin/forms/{id}` | Get form details |
| `PUT` | `/api/admin/forms/{id}` | Update a form |
| `DELETE` | `/api/admin/forms/{id}` | Soft-delete a form |
| `GET` | `/api/admin/forms/{id}/info` | Get form summary information |
| `GET` | `/api/admin/forms/{id}/linkable-forms` | List forms that can be linked |
| `GET` | `/api/admin/forms/{id}/draft` | Get a form editing draft |
| `POST` | `/api/admin/forms/{id}/draft` | Save a form editing draft |
| `DELETE` | `/api/admin/forms/{id}/draft` | Delete a form editing draft |
| `GET` | `/api/admin/forms/{id}/responses` | List form responses |
| `GET` | `/api/admin/forms/{id}/responses/export` | Export responses to Excel |
| `GET` | `/api/admin/forms/{id}/metrics` | Get form metrics |
| `GET` | `/api/admin/forms/{id}/analytics` | Get answer analytics |
| `GET` | `/api/admin/forms/metrics` | Get service-wide metrics |
| `GET` | `/api/admin/forms/responses/{id}` | Get one response |
| `POST` | `/api/admin/forms/responses/{id}/share` | Create or refresh a response share token |
| `POST` | `/api/admin/forms/responses/{id}/revoke-token` | Revoke a response share token |
| `PATCH` | `/api/admin/forms/responses/{id}/status` | Update response review status |
| `POST` | `/api/admin/forms/responses/{id}/archive` | Archive a response |

### Component Groups - Admin

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/admin/forms/component-groups` | List component groups |
| `GET` | `/api/admin/forms/component-groups/{id}` | Get component-group details |
| `POST` | `/api/admin/forms/component-groups` | Create a component group |
| `PUT` | `/api/admin/forms/component-groups/{id}` | Update a component group |
| `DELETE` | `/api/admin/forms/component-groups/{id}` | Delete a component group |
| `POST` | `/api/admin/forms/component-groups/{id}/share` | Create or refresh a share token |
| `POST` | `/api/admin/forms/component-groups/{id}/clone` | Clone a shared component group |

## Authentication & Authorization

- **Current user:** The API parses the forwarded Bearer token to resolve the current user ID and client roles.
- **External user data:** User details are fetched from the `super-skylab` service through an Application abstraction and Infrastructure HTTP adapter.
- **Service authentication:** SkyMail calls use a Keycloak client-credentials token.
- **Authorization:** Role-based rules are enforced in the Application services:
  - **Owner** - Full control and collaborator management
  - **Editor** - Edit forms and manage responses
  - **Viewer** - Read-only access

## Getting Started

### Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [PostgreSQL](https://www.postgresql.org/download/)
- [Redis](https://redis.io/)

### Environment Setup

Create the local Compose environment file from the tracked template:

```powershell
# PowerShell
Copy-Item .env.example .env
```

```bash
# Bash
cp .env.example .env
```

Edit `.env` before starting Docker. The local `.env` file is ignored by Git.

### Running Locally

Provide `CONNECTION_STRING`, `Redis__ConnectionString`, and any required integration configuration through environment variables or an ignored `src/Forms.Api/appsettings.Development.json`.

```bash
# Restore dependencies
dotnet restore src/Forms.sln

# Build
dotnet build src/Forms.sln

# Run the application
dotnet run --project src/Forms.Api
```

Database migrations run automatically on startup. Swagger UI is available at the application's `/swagger` path.

### Docker Compose

```bash
docker compose up --build
```

The Compose stack starts PostgreSQL, Redis, and Forms API. It expects the external `skynet` network because Eureka, `super-skylab`, and SkyMail are external services.

### Docker Image

```bash
docker build -f src/Dockerfile -t skylab-forms-api src
```

## Configuration

| Variable | Description | Required |
|----------|-------------|----------|
| `CONNECTION_STRING` | PostgreSQL connection string for local/non-Compose execution | Yes |
| `Redis__ConnectionString` | Redis connection string | No, defaults to `localhost:6379` |
| `ALLOWED_ORIGIN` | CORS allowed origin | No, defaults to `http://localhost:3000` |
| `KEYCLOAK_TOKEN_URL` | Keycloak token endpoint used by Compose | For mail integration |
| `KEYCLOAK_CLIENT_ID` | Keycloak service client ID | For mail integration |
| `KEYCLOAK_CLIENT_SECRET` | Keycloak service client secret | For mail integration |
| `FORMMAIL_FORM_COPY_TEMPLATE_ID` | Submitted-form copy template | Optional |
| `FORMMAIL_STATUS_CHANGED_TEMPLATE_ID` | Review status template | Optional |
| `FORMMAIL_PENDING_REMINDER_TEMPLATE_ID` | Pending response reminder template | Optional |

Database access uses an automatic retry strategy with five retries and a maximum ten-second delay.

## Database Migrations

```bash
dotnet ef migrations add MigrationName \
  --project src/Forms.Infrastructure \
  --startup-project src/Forms.Api
```

## Validation

```bash
dotnet build src/Forms.sln -c Release
dotnet list src/Forms.sln package --vulnerable --include-transitive
```

See [CONTRIBUTING.md](CONTRIBUTING.md) for architectural boundaries and contribution rules.