# User Tasks Module API

ASP.NET Core REST API implementation of the onboarding/compliance `Tasks + Steps` workflow described in the challenge README.

The solution enforces linear dependencies, supports manual and automated steps, provides admin override capabilities, uses SQLite via EF Core, includes tests, and is containerized with Docker.

## Implemented Requirements

- ASP.NET Core Web API (`net10.0`, satisfies "8 or later")
- EF Core ORM with relational SQLite
- Domain entities:
  - `Task`: title, description, icon, assigned user, status (`OPEN`/`DONE`)
  - `Step`: title, description, icon, status (`OPEN`/`DONE`), order index, type (`Manual`/`Automated`), required flag, module key
- Admin capabilities:
  - CRUD for tasks and steps
  - assign task to user
  - force task/step status to `OPEN` or `DONE` (override)
- User capabilities:
  - fetch assigned tasks with locked-state indicators
  - fetch task details with per-step lock/completion state
  - complete manual steps (dependency-protected)
- System logic:
  - mock event endpoint for `module_entry_created`
  - completes matching unlocked automated step for `userId + moduleKey`
- Tests project with workflow/unit tests
- Dockerfile for containerized run

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
Dockerfile
```

## Workflow Rules

- Steps are ordered by `OrderIndex`.
- A step is **locked** if any preceding `IsRequired = true` step is not `DONE`.
- Users can only complete `Manual` steps.
- Completing a locked step returns `403 Forbidden`.
- Automated completion is only possible via mock event endpoint.
- Task status is automatically recalculated from step statuses unless admin forced task status.

## API Endpoints

All routes are prefixed with `/api`.

### Admin: Tasks

- `GET /admin/tasks`
- `GET /admin/tasks/{taskId}`
- `POST /admin/tasks`
- `PUT /admin/tasks/{taskId}`
- `DELETE /admin/tasks/{taskId}`
- `POST /admin/tasks/{taskId}/assign`
- `POST /admin/tasks/{taskId}/force-status`
- `POST /admin/tasks/{taskId}/status/reset` (release override and recalculate)

### Admin: Steps

- `GET /admin/tasks/{taskId}/steps`
- `POST /admin/tasks/{taskId}/steps`
- `GET /admin/steps/{stepId}`
- `PUT /admin/steps/{stepId}`
- `DELETE /admin/steps/{stepId}`
- `POST /admin/steps/{stepId}/force-status`

### User

- `GET /users/{userId}/tasks`
- `GET /users/{userId}/tasks/{taskId}`
- `POST /users/{userId}/steps/{stepId}/complete`

### Mock Event Consumer

- `POST /mock/events/module-entry-created`

Request body:

```json
{
  "userId": "user-123",
  "moduleKey": "profile"
}
```

## Enum Values

- `TaskStatus`: `OPEN`, `DONE`
- `StepStatus`: `OPEN`, `DONE`
- `StepType`: `Manual`, `Automated`

## Local Setup

1. Restore API dependencies:

```bash
dotnet restore BackendApplication.Api/BackendApplication.Api.csproj
```

2. Restore test dependencies:

```bash
dotnet restore BackendApplication.Tests/BackendApplication.Tests.csproj
```

3. Run API:

```bash
dotnet run --project BackendApplication.Api
```

3. OpenAPI (development):
- `/openapi/v1.json`

SQLite DB is created automatically on startup using:
- `ConnectionStrings:DefaultConnection`
- Default value: `Data Source=data/app.db`

## Testing

Run all tests:

```bash
dotnet test BackendApplication.Tests/BackendApplication.Tests.csproj
```

Tests cover:
- required-step dependency enforcement
- manual completion success/failure paths
- automated event completion behavior

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

## Notes on Mockup Alignment

The response payloads include summary and step-state fields needed by the provided UI mockups (`assets/tasks.png`, `assets/steps.png`), including:
- task progress fields (`total/completed/remaining`)
- next actionable step
- per-step `isLocked` and `canCompleteManually`
