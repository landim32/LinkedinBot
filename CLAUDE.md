# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run

```bash
dotnet build                                    # Build all projects
dotnet run --project LinkedinBot.Console        # Run interactive console
dotnet run --project LinkedinBot.Worker         # Run headless worker
docker compose up --build                       # Run Worker + Postgres in Docker
docker compose --profile console up linkedinbot --build  # Run Console in Docker
```

**Playwright install (Windows):**
```bash
powershell -Command "playwright install chromium"
```

**EF Core migrations:**
```bash
# Postgres
dotnet ef migrations add <Name> --project LinkedinBot.Infra.Postgres --startup-project LinkedinBot.Console --context JobHistoryDbContext --output-dir Data/Migrations
# Sqlite
dotnet ef migrations add <Name> --project LinkedinBot.Infra.Sqlite --startup-project LinkedinBot.Console --context SqliteJobHistoryDbContext --output-dir Data/Migrations
```

There are no tests in this project.

## Architecture

Clean Architecture with 10 projects. Dependency flow: Console/Worker → Application → [Infra + Infra.Json + Infra.Postgres + Infra.Sqlite] → Infra.Interfaces → Domain → DTO.

| Project | Role | Key Files |
|---------|------|-----------|
| **DTO** | POCOs, settings classes, no dependencies | `Models/AppSettings.cs`, `Models/JobListing.cs`, `Models/CompatibilityResult.cs` |
| **Domain** | Business logic + service interfaces | `Services/JobAnalyzerService.cs` (60% confidence threshold), `Services/Interfaces/IChatGptService.cs` |
| **Infra.Interfaces** | Infrastructure contracts, uses `IPage` from Playwright | `AppServices/IBrowserAppService.cs`, `IJobHistoryAppService.cs`, `ILinkedIn*AppService.cs` |
| **Infra** | Playwright + OpenAI AppServices | `AppServices/` (Browser, Auth, Search, Apply, ChatGpt), `Constants/Selectors.cs` |
| **Infra.Json** | JSON file-based job history | `Repositories/JsonJobHistoryRepository.cs` |
| **Infra.Postgres** | Postgres job history (EF Core + Npgsql) | `Repositories/PostgresJobHistoryRepository.cs`, `Data/JobHistoryDbContext.cs` |
| **Infra.Sqlite** | SQLite job history (EF Core) | `Repositories/SqliteJobHistoryRepository.cs`, `Data/SqliteJobHistoryDbContext.cs` |
| **Application** | DI wiring only | `Initializer.cs` — `RegisterServices()` extension method |
| **Console** | Interactive entry point with Ctrl+C handling | `Program.cs` — orchestration loop with interactive pause |
| **Worker** | BackgroundService for Docker/headless | `Program.cs`, `LinkedInBotWorker.cs` — scoped DbContext per cycle |

### Key Design Decisions

- **`IChatGptService` lives in Domain** (not Infra.Interfaces) because `JobAnalyzerService` depends on it — Dependency Inversion Principle.
- **`IPage` (Playwright) exposed in Infra.Interfaces** — pragmatic choice since Console/Worker orchestrate browser interaction directly.
- **Console/Worker never reference Infra projects directly** — only Infra.Interfaces for type definitions.
- **Infra.Interfaces depends only on DTO** — no dependency on Domain or Infra.
- **Four Infra projects**: `Infra` (browser/AI AppServices), `Infra.Json` (file persistence), `Infra.Postgres` (Postgres persistence), `Infra.Sqlite` (SQLite persistence).
- **Job history provider is config-selectable** via `DataConnection:Provider`: `sqlite` (default) | `postgres` | `json`.
- **All persistence config in one section** (`DataConnection`): `Provider`, `ConnectionString`, `FilePath`.
- **`InteractivePrompt` config** controls unrecognized form behavior: `true` (Console) pauses for user input, `false` (Worker) skips.
- **Selectors centralized** in `Infra/Constants/Selectors.cs` — update here when LinkedIn changes DOM.

### DI Lifetimes

- **Singleton:** `ChatGptService`, `BrowserService`, `JsonJobHistoryRepository`
- **Transient:** LinkedIn services (`Auth`, `Search`, `Apply`), `JobAnalyzerService`
- **Scoped:** `PostgresJobHistoryRepository`, `SqliteJobHistoryRepository` (per-scope DbContext)

## Configuration

All settings use `IOptions<T>` pattern. Section names: `LinkedIn`, `OpenAI`, `JobSearch`, `Browser`, `DataConnection`, `Resume`. All persistence-related settings are consolidated under `DataConnection`:

```json
"DataConnection": {
  "Provider": "sqlite",              // sqlite | postgres | json
  "FilePath": "",                     // used by json provider only
  "ConnectionString": "Data Source=job-history.db"  // used by sqlite/postgres providers
}
```

Sensitive files (`appsettings.json`, `resume.md`) are gitignored — only `.example` variants are tracked.

## Conventions

- Async methods suffixed with `Async`
- Private readonly fields prefixed with `_`
- All LinkedIn DOM selectors go in `Selectors.cs` (pt-BR localized button text)
- Structured logging with Serilog: `Information` for user-visible actions, `Debug` for details, `Warning` for recoverable errors
- Use the `/dotnet-architecture` skill when creating new entities, services, or repositories — it covers all layers in order

## Docker

Single parametrized `Dockerfile` with `ARG PROJECT` (defaults to Console). `docker-compose.yml` services: `postgres` (16-alpine), `worker` (auto-migrates on startup), `linkedinbot` (console, optional profile). Worker overrides: `DataConnection__Provider=postgres`, `DataConnection__ConnectionString=...`, `Browser__Headless=true`, `Browser__Channel=chromium`.

## Versioning

GitVersion with ContinuousDelivery mode. Commit message prefixes trigger bumps: `major:`/`breaking` → major, `feat:` → minor, `fix:` → patch.
