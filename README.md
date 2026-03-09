# Backend Application - User Tasks Module API

This repository contains a complete ASP.NET Core Web API implementation of the **User Tasks Module** described in [TASK.md](TASK.md).

The service handles onboarding/compliance workflows where each user has assigned tasks composed of ordered steps, with dependency checks, manual completion, and automated event-driven completion.

## What We Built

- REST API using ASP.NET Core (`net10.0`, compatible with "8 or later" requirement).
- Relational persistence using EF Core + SQLite.
- Full domain workflow for:
  - tasks assigned to users
  - ordered steps (manual/automated)
  - required-step dependency locking
  - task/step status transitions (`OPEN` / `DONE`)
- Admin capabilities:
  - CRUD for tasks and steps
  - assignment updates
  - emergency force status override
- User capabilities:
  - fetch assigned tasks and detailed step state
  - manual step completion with dependency validation
- Mock event consumer endpoint:
  - simulate `module_entry_created`
  - auto-complete matching active automated step
- Unit/integration-style workflow tests with SQLite in-memory DB.
- Dockerized runtime via `Dockerfile`.

## Core Workflow Logic

- Steps are processed in ascending `OrderIndex`.
- A step is considered **locked** when any preceding **required** step is not `DONE`.
- Users can complete only `Manual` steps.
- Automated steps are completed only through the mock event endpoint.
- Task status is recalculated from step statuses unless task status was force-overridden by admin.

## Tech Stack

- ASP.NET Core Web API
- Entity Framework Core
- SQLite
- xUnit
- Docker

## Project Structure

```text
BackendApplication.sln
BackendApplication.Api/
  Controllers/
  Contracts/
  Data/
  Domain/
  Services/
BackendApplication.Tests/
TASK.md
README.md
Dockerfile
```

## API Surface

All endpoints are under `/api`.

### Admin - Task Management

- `GET /api/admin/tasks`
- `GET /api/admin/tasks/{taskId}`
- `POST /api/admin/tasks`
- `PUT /api/admin/tasks/{taskId}`
- `DELETE /api/admin/tasks/{taskId}`
- `POST /api/admin/tasks/{taskId}/assign`
- `POST /api/admin/tasks/{taskId}/force-status`
- `POST /api/admin/tasks/{taskId}/status/reset`

### Admin - Step Management

- `GET /api/admin/tasks/{taskId}/steps`
- `POST /api/admin/tasks/{taskId}/steps`
- `GET /api/admin/steps/{stepId}`
- `PUT /api/admin/steps/{stepId}`
- `DELETE /api/admin/steps/{stepId}`
- `POST /api/admin/steps/{stepId}/force-status`

### User

- `GET /api/users/{userId}/tasks`
- `GET /api/users/{userId}/tasks/{taskId}`
- `POST /api/users/{userId}/steps/{stepId}/complete`

### Mock Event Endpoint

- `POST /api/mock/events/module-entry-created`

Request body:

```json
{
  "userId": "user-123",
  "moduleKey": "profile"
}
```

## Quick Start (Local)

### Prerequisites

- .NET SDK (8+; developed with .NET 10 SDK)

### 1) Restore dependencies

```bash
dotnet restore BackendApplication.Api/BackendApplication.Api.csproj
dotnet restore BackendApplication.Tests/BackendApplication.Tests.csproj
```

### 2) Run API

```bash
dotnet run --project BackendApplication.Api
```

To run on the same port used in validation:

```bash
dotnet run --project BackendApplication.Api --urls http://127.0.0.1:5096
```

### 3) OpenAPI (Development)

- `GET /openapi/v1.json`
- Full URL (when running on port `5096`): `http://127.0.0.1:5096/openapi/v1.json`

## Configuration

`BackendApplication.Api/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=data/app.db"
  }
}
```

The database is created automatically at startup.

Additional useful settings:

- `ASPNETCORE_ENVIRONMENT=Development` enables OpenAPI mapping in this project.
- SQLite path is relative to API content root (`BackendApplication.Api`), so `Data Source=data/app.db` resolves to `BackendApplication.Api/data/app.db`.

## Quick Smoke Flow (Manual Validation)

1. Create task:

```bash
curl -X POST http://127.0.0.1:5096/api/admin/tasks \
  -H "Content-Type: application/json" \
  -d "{\"title\":\"Design\",\"description\":\"Onboarding\",\"icon\":\"icon\",\"assignedUserId\":\"user-1\"}"
```

2. Add step 1 (manual):

```bash
curl -X POST http://127.0.0.1:5096/api/admin/tasks/{taskId}/steps \
  -H "Content-Type: application/json" \
  -d "{\"title\":\"Manual\",\"description\":\"Fill profile\",\"icon\":\"profile\",\"type\":\"Manual\",\"isRequired\":true,\"orderIndex\":1}"
```

3. Add step 2 (automated):

```bash
curl -X POST http://127.0.0.1:5096/api/admin/tasks/{taskId}/steps \
  -H "Content-Type: application/json" \
  -d "{\"title\":\"Auto\",\"description\":\"Module event\",\"icon\":\"event\",\"type\":\"Automated\",\"isRequired\":true,\"orderIndex\":2,\"moduleKey\":\"profile\"}"
```

4. Complete manual step:

```bash
curl -X POST http://127.0.0.1:5096/api/users/user-1/steps/{step1Id}/complete \
  -H "Content-Type: application/json" \
  -d "{}"
```

5. Trigger automated event:

```bash
curl -X POST http://127.0.0.1:5096/api/mock/events/module-entry-created \
  -H "Content-Type: application/json" \
  -d "{\"userId\":\"user-1\",\"moduleKey\":\"profile\"}"
```

6. Verify task is done:

```bash
curl http://127.0.0.1:5096/api/users/user-1/tasks/{taskId}
```

## Testing

Run all tests:

```bash
dotnet test BackendApplication.Tests/BackendApplication.Tests.csproj
```

Current test coverage includes:

- dependency validation for manual completion
- successful manual completion and task status update
- automated-step rejection in manual endpoint path
- event-driven automated completion
- no-match behavior when automated step is locked

Current executed test result:

- `Passed: 5, Failed: 0`

## Docker

Build image:

```bash
docker build -t backend-application .
```

Run container:

```bash
docker run --rm -p 8080:8080 backend-application
```

Container defaults:

- `ASPNETCORE_URLS=http://+:8080`
- `ConnectionStrings__DefaultConnection=Data Source=/app/data/app.db`

## Notes

- Challenge context and original specification are in [TASK.md](TASK.md).
- UI mockup references used for response-shape alignment:
  - `assets/tasks.png`
  - `assets/steps.png`
- Authentication/authorization is intentionally not implemented in this challenge scope.
