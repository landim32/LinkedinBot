# LinkedinBot - LinkedIn Easy Apply Automation

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)
![Playwright](https://img.shields.io/badge/Playwright-1.49.0-2EAD33?logo=playwright)
![OpenAI](https://img.shields.io/badge/OpenAI-GPT--4o-412991?logo=openai)
![License](https://img.shields.io/badge/License-MIT-green)

## Overview

**LinkedinBot** is an intelligent LinkedIn job application automation tool that uses **Playwright** for browser automation and **OpenAI GPT-4o** for AI-powered decision making. It automatically searches for jobs, evaluates compatibility with your resume, fills out Easy Apply forms, and submits applications — all with minimal manual intervention.

Built with **.NET 8.0** following **Clean Architecture** principles across 10 layered projects, with pluggable persistence (JSON, PostgreSQL, or SQLite), Docker support, and automatic versioning via GitVersion.

---

## 🚀 Features

- 🤖 **AI-Powered Job Analysis** — Uses GPT-4o to evaluate job compatibility against your resume with a configurable confidence threshold (default: 60%)
- 📝 **Smart Form Filling** — Automatically fills text inputs, textareas, selects, and radio groups using AI-generated answers
- 🔍 **Advanced Job Search** — Configurable search with keywords, boolean operators, location (GeoId), distance, experience level, and remote filters
- 🔄 **Continuous Execution** — Runs in a loop with configurable intervals, automatically searching for and applying to new jobs
- 💾 **Pluggable Persistence** — Choose between JSON file, PostgreSQL, or SQLite for job history storage via a single config switch
- 🐳 **Docker Ready** — Worker (headless BackgroundService) + Console modes, with Docker Compose for Postgres + Worker
- 🛡️ **Safety Dialog Handling** — Automatically dismisses LinkedIn's security reminder dialogs
- ⏸️ **Interactive Pause** — Pauses on unrecognized form elements, allowing manual resolution before continuing (Console mode)
- 🚫 **Job Dismissal** — Automatically dismisses incompatible jobs from the feed
- 📜 **Dynamic Scrolling** — Scrolls through entire job list pages to load all lazy-loaded content
- 🔢 **Numeric Field Sanitization** — Detects input types and sanitizes numeric fields (salary, phone, etc.)
- 🌐 **Multi-Language Placeholder Detection** — Recognizes select placeholders in Portuguese, Spanish, and English
- 📋 **Detailed Logging** — Step-by-step logging of every form action with Serilog (console + file)
- 🏷️ **Automatic Versioning** — GitVersion with ContinuousDelivery mode, auto-tagging and GitHub Releases on push to main

---

## ⚡ Quick Start

### 1. Set up your credentials

Copy the template and edit `appsettings.json`:

```bash
cp appsettings.example.json LinkedinBot.Console/appsettings.json
```

Open `LinkedinBot.Console/appsettings.json` and fill in your credentials:

```json
{
  "LinkedIn": {
    "Email": "your-email@example.com",
    "Password": "your-linkedin-password"
  },
  "OpenAI": {
    "ApiKey": "sk-proj-YOUR-OPENAI-KEY-HERE",
    "Model": "gpt-4o"
  },
  "JobSearch": {
    "Keywords": "(.NET OR C#) AND NOT Java",
    "SalaryExpectation": 15000
  }
}
```

> You can get your API key at [platform.openai.com/api-keys](https://platform.openai.com/api-keys). The remaining `JobSearch` and `Browser` settings can be left at their default values from the template.

### 2. Fill in your resume in `resume.md`

Copy the template and edit with your information:

```bash
cp resume.example.md LinkedinBot.Console/resume.md
```

Open `LinkedinBot.Console/resume.md` and follow this structure:

```markdown
# Your Full Name

**Your Title (e.g., Senior Developer)**

City, State | (555) 123-4567 | email@example.com

---

## Professional Summary

Summary of your experience, key skills, and career goals.
The bot uses this content to evaluate job compatibility and answer
Easy Apply form questions.

---

## Professional Experience

### Job Title — Company
**Period** | Location

- Key achievement or responsibility
- Key achievement or responsibility

**Technologies:** .NET, C#, SQL Server, Azure...

---

## Education

- **Degree** — University *(Period)*
```

> The more complete your resume, the better the AI will perform when filling out forms and evaluating job compatibility.

### 3. Run the bot

```bash
dotnet build
dotnet run --project LinkedinBot.Console
```

The bot will open Chrome, log into LinkedIn, search for jobs using your configured filters, and automatically apply to positions that match your profile. The cycle repeats every `SearchIntervalMinutes` (default: 5 min).

> On the first run, Playwright may require browser installation. Run: `powershell -Command "playwright install chromium"`

---

## 🛠️ Technologies Used

### Core Framework
- **.NET 8.0** — Target framework with implicit usings and nullable reference types

### Browser Automation
- **Microsoft.Playwright 1.49.0** — Browser automation with persistent Chrome context for session persistence

### AI Integration
- **OpenAI SDK 2.1.0** — GPT-4o integration for job compatibility analysis and intelligent form filling

### Persistence
- **Entity Framework Core 8.0.11** — ORM for PostgreSQL and SQLite providers
- **Npgsql.EntityFrameworkCore.PostgreSQL 8.0.11** — PostgreSQL provider
- **Microsoft.EntityFrameworkCore.Sqlite 8.0.11** — SQLite provider
- **System.Text.Json** — JSON file-based persistence (no external dependencies)

### Logging
- **Serilog** — Structured logging with console and daily-rolling file sinks

### Dependency Injection & Configuration
- **Microsoft.Extensions.Hosting 8.0.1** — Host builder with DI container and configuration
- **Microsoft.Extensions.DependencyInjection** — Service registration and lifetime management
- **Microsoft.Extensions.Options** — Strongly-typed settings binding from `appsettings.json`

### Versioning & CI/CD
- **GitVersion** — Semantic versioning with ContinuousDelivery mode
- **GitHub Actions** — Auto-tagging and release creation

---

## 📁 Project Structure

```
LinkedinBot/
├── LinkedinBot.DTO/                    # Data Transfer Objects (no dependencies)
│   ├── Models/
│   │   ├── AppSettings.cs              # All configuration POCOs
│   │   ├── JobListing.cs               # Job data model
│   │   ├── CompatibilityResult.cs      # AI analysis result
│   │   ├── ApplicationResult.cs        # Application outcome
│   │   └── JobHistoryEntry.cs          # Persistent history entry
│   └── Exceptions/
│       └── UnrecognizedFormActionException.cs
│
├── LinkedinBot.Domain/                 # Business logic (→ DTO)
│   └── Services/
│       ├── Interfaces/
│       │   ├── IChatGptService.cs      # AI service contract
│       │   └── IJobAnalyzerService.cs  # Job evaluation contract
│       └── JobAnalyzerService.cs       # Compatibility analysis (60% threshold)
│
├── LinkedinBot.Infra.Interfaces/       # Infrastructure contracts (→ DTO)
│   └── AppServices/
│       ├── IBrowserAppService.cs       # Playwright browser lifecycle
│       ├── ILinkedInAuthAppService.cs  # LinkedIn session management
│       ├── ILinkedInSearchAppService.cs # Job collection & dismissal
│       ├── ILinkedInApplyAppService.cs # Easy Apply form automation
│       └── IJobHistoryAppService.cs    # Job history persistence
│
├── LinkedinBot.Infra/                  # Implementations (→ Infra.Interfaces, Domain, DTO)
│   ├── AppServices/
│   │   ├── BrowserAppService.cs        # Playwright initialization
│   │   ├── LinkedInAuthAppService.cs   # Login with session persistence
│   │   ├── LinkedInSearchAppService.cs # Search URL builder & job scraping
│   │   ├── LinkedInApplyAppService.cs  # Form filling & submission
│   │   └── ChatGptAppService.cs        # OpenAI API integration
│   └── Constants/
│       └── Selectors.cs                # Centralized LinkedIn DOM selectors
│
├── LinkedinBot.Infra.Json/             # JSON file persistence (→ Infra.Interfaces, DTO)
│   └── Repositories/
│       └── JsonJobHistoryRepository.cs
│
├── LinkedinBot.Infra.Postgres/         # PostgreSQL persistence (→ Infra.Interfaces, DTO)
│   ├── Data/
│   │   ├── JobHistoryDbContext.cs
│   │   └── Migrations/
│   └── Repositories/
│       └── PostgresJobHistoryRepository.cs
│
├── LinkedinBot.Infra.Sqlite/           # SQLite persistence (→ Infra.Interfaces, DTO)
│   ├── Data/
│   │   ├── SqliteJobHistoryDbContext.cs
│   │   └── Migrations/
│   └── Repositories/
│       └── SqliteJobHistoryRepository.cs
│
├── LinkedinBot.Application/            # DI wiring (→ all projects)
│   └── Initializer.cs                  # Service registration & config binding
│
├── LinkedinBot.Console/                # Interactive entry point (→ Application, Infra.Interfaces, Domain, DTO)
│   ├── Program.cs                      # Main orchestration loop
│   ├── appsettings.example.json        # Configuration template
│   └── resume.example.md              # Resume template
│
├── LinkedinBot.Worker/                 # Headless BackgroundService (→ Application, Infra.Interfaces, Domain, DTO)
│   ├── Program.cs                      # Hosted service setup
│   ├── LinkedInBotWorker.cs            # Background worker with scoped DbContext per cycle
│   └── appsettings.example.json        # Worker configuration template
│
├── .github/workflows/
│   ├── version-tag.yml                 # Auto-tag on push to main
│   └── create-release.yml              # Auto-release on minor/major version bumps
│
├── docker-compose.yml                  # Postgres + Worker + Console services
├── Dockerfile                          # Multi-stage build (SDK → Playwright runtime)
├── GitVersion.yml                      # Semantic versioning config
├── appsettings.example.json            # Root configuration template
├── resume.example.md                   # Resume template
├── .gitignore
├── LICENSE                             # MIT License
└── README.md
```

### Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│           LinkedinBot.Console / LinkedinBot.Worker                   │
│                    (Entry Points)                                    │
└──────────────────────────────┬──────────────────────────────────────┘
                               │
┌──────────────────────────────▼──────────────────────────────────────┐
│                    LinkedinBot.Application                           │
│                 (Initializer.cs — DI Wiring)                        │
└──────────────────────────────┬──────────────────────────────────────┘
                               │
      ┌──────────┬─────────────┼─────────────┬──────────┐
      ▼          ▼             ▼             ▼          ▼
┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐
│  .Infra  │ │.Infra.Json│ │.Infra.   │ │.Infra.   │ │ .Domain  │
│ Browser, │ │  JSON     │ │ Postgres │ │ Sqlite   │ │ Services │
│ AI, Auth │ │ Storage   │ │ EF Core  │ │ EF Core  │ │ Rules    │
└────┬─────┘ └────┬─────┘ └────┬─────┘ └────┬─────┘ └────┬─────┘
     │             │            │             │            │
     └─────────────┴────────────┴──────┬──────┴────────────┘
                                       ▼
                          ┌────────────────────────┐
                          │  .Infra.Interfaces     │
                          │  (Contracts + IPage)   │
                          └────────────┬───────────┘
                                       ▼
                              ┌─────────────────┐
                              │ LinkedinBot.DTO │
                              │ (Models/POCOs)  │
                              └─────────────────┘
```

---

## ⚙️ Environment Configuration

### 1. Copy the configuration templates

```bash
# For Console mode
cp appsettings.example.json LinkedinBot.Console/appsettings.json
cp resume.example.md LinkedinBot.Console/resume.md

# For Worker mode (Docker)
cp appsettings.example.json LinkedinBot.Worker/appsettings.json
cp resume.example.md LinkedinBot.Worker/resume.md
```

### 2. Edit `appsettings.json`

```json
{
  "LinkedIn": {
    "Email": "your-email@example.com",
    "Password": "your-linkedin-password"
  },
  "OpenAI": {
    "ApiKey": "sk-proj-your-openai-api-key-here",
    "Model": "gpt-4o"
  },
  "JobSearch": {
    "Keywords": "(.NET OR C#) AND NOT Java",
    "GeoId": "106057199",
    "Distance": 25,
    "ExperienceLevel": "4",
    "EasyApply": true,
    "RemoteFilter": "2",
    "MaxApplicationsPerRun": 50,
    "SearchIntervalMinutes": 5,
    "SalaryExpectation": 15000,
    "MaxFormSteps": 20,
    "InteractivePrompt": true
  },
  "DataConnection": {
    "Provider": "sqlite",
    "FilePath": "",
    "ConnectionString": "Data Source=job-history.db"
  },
  "Browser": {
    "Locale": "pt-BR",
    "Headless": false,
    "SlowMo": 500,
    "UserDataDir": "./user-data",
    "Channel": "chrome"
  },
  "Resume": {
    "MarkdownPath": "./resume.md"
  }
}
```

### 3. Edit `resume.md`

Write your resume in Markdown format. This is used by the AI to evaluate job compatibility and answer form questions. See `resume.example.md` for the expected structure.

### Configuration Reference

| Section | Setting | Description | Default |
|---------|---------|-------------|---------|
| **LinkedIn** | `Email` | Your LinkedIn email | — |
| **LinkedIn** | `Password` | Your LinkedIn password | — |
| **OpenAI** | `ApiKey` | OpenAI API key | — |
| **OpenAI** | `Model` | GPT model to use | `gpt-4o` |
| **JobSearch** | `Keywords` | Search keywords (supports boolean operators) | — |
| **JobSearch** | `GeoId` | LinkedIn geographic ID | `106057199` |
| **JobSearch** | `Distance` | Search radius in km | `25` |
| **JobSearch** | `ExperienceLevel` | LinkedIn experience filter (`4` = Mid-Senior) | `4` |
| **JobSearch** | `EasyApply` | Only show Easy Apply jobs | `true` |
| **JobSearch** | `RemoteFilter` | Remote work filter (`2` = Remote) | `2` |
| **JobSearch** | `MaxApplicationsPerRun` | Max applications per cycle | `50` |
| **JobSearch** | `SearchIntervalMinutes` | Minutes between search cycles | `5` |
| **JobSearch** | `SalaryExpectation` | Salary value for form fields | `15000` |
| **JobSearch** | `MaxFormSteps` | Max form steps before pausing | `20` |
| **JobSearch** | `InteractivePrompt` | Pause on unrecognized forms (`true` for Console, `false` for Worker) | `true` |
| **DataConnection** | `Provider` | Persistence provider: `sqlite`, `postgres`, or `json` | `sqlite` |
| **DataConnection** | `FilePath` | Path to JSON history file (only for `json` provider) | — |
| **DataConnection** | `ConnectionString` | Database connection string (for `sqlite` or `postgres` providers) | `Data Source=job-history.db` |
| **Browser** | `Locale` | Browser locale | `pt-BR` |
| **Browser** | `Headless` | Run browser without UI | `false` |
| **Browser** | `SlowMo` | Delay between actions (ms) | `500` |
| **Browser** | `UserDataDir` | Path to persist browser session | `./user-data` |
| **Browser** | `Channel` | Browser channel (`chrome` for local, `chromium` for Docker) | `chrome` |
| **Resume** | `MarkdownPath` | Path to your resume.md | `./resume.md` |

---

## 💾 Data Connection (Persistence)

The bot tracks which jobs have been analyzed to avoid re-processing. All persistence settings are consolidated under the `DataConnection` section in `appsettings.json`.

### JSON (default)

Stores job history in a local JSON file. Best for local/Console usage — no database required.

```json
"DataConnection": {
  "Provider": "json",
  "FilePath": "./job-history.json",
  "ConnectionString": ""
}
```

- File is created automatically on first run
- DI lifetime: **Singleton** (in-memory cache + file persistence)
- No external dependencies

### PostgreSQL

Stores job history in a PostgreSQL database. Recommended for Docker/Worker deployments.

```json
"DataConnection": {
  "Provider": "postgres",
  "FilePath": "",
  "ConnectionString": "Host=localhost;Port=5432;Database=linkedinbot;Username=linkedinbot;Password=your_secure_password_here"
}
```

- Migrations are **auto-applied** on startup via `MigrateAsync()`
- DI lifetime: **Scoped** (per-scope DbContext)
- Requires a running PostgreSQL instance (see [Docker Setup](#-docker-setup))

### SQLite

Stores job history in a local SQLite database file. Good middle ground — structured storage without a separate database server.

```json
"DataConnection": {
  "Provider": "sqlite",
  "FilePath": "",
  "ConnectionString": "Data Source=job-history.db"
}
```

- Database file is created automatically
- Migrations are **auto-applied** on startup via `MigrateAsync()`
- DI lifetime: **Scoped** (per-scope DbContext)
- No external server required

### EF Core Migrations

If you modify the database schema, generate new migrations:

```bash
# PostgreSQL
dotnet ef migrations add <MigrationName> \
  --project LinkedinBot.Infra.Postgres \
  --startup-project LinkedinBot.Console \
  --context JobHistoryDbContext \
  --output-dir Data/Migrations

# SQLite
dotnet ef migrations add <MigrationName> \
  --project LinkedinBot.Infra.Sqlite \
  --startup-project LinkedinBot.Console \
  --context SqliteJobHistoryDbContext \
  --output-dir Data/Migrations
```

> Requires `dotnet-ef` tool: `dotnet tool install --global dotnet-ef`

---

## 🔧 Setup

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Google Chrome](https://www.google.com/chrome/) (or Chromium)
- [OpenAI API Key](https://platform.openai.com/api-keys)
- A LinkedIn account

### Installation

#### 1. Clone the repository

```bash
git clone https://github.com/landim32/LinkedinBot.git
cd LinkedinBot
```

#### 2. Install Playwright browsers

```bash
powershell -Command "playwright install chromium"
```

#### 3. Configure the application

```bash
cp appsettings.example.json LinkedinBot.Console/appsettings.json
cp resume.example.md LinkedinBot.Console/resume.md
```

Edit both files with your credentials and resume (see [Environment Configuration](#️-environment-configuration)).

#### 4. Build and run

```bash
dotnet build
dotnet run --project LinkedinBot.Console
```

---

## 🐳 Docker Setup

The project includes two Docker targets: a **Worker** (BackgroundService, headless, Postgres) and the **Console** (interactive prompt).

### Prerequisites

- [Docker](https://docs.docker.com/get-docker/) and [Docker Compose](https://docs.docker.com/compose/install/)

### Worker (recommended for Docker)

The Worker runs as a BackgroundService with PostgreSQL for job history persistence. Unrecognized form elements are automatically skipped (no interactive prompt).

#### 1. Configure

```bash
cp appsettings.example.json LinkedinBot.Worker/appsettings.json
cp resume.example.md LinkedinBot.Worker/resume.md
```

Edit `LinkedinBot.Worker/appsettings.json` — set your LinkedIn/OpenAI credentials. The Worker defaults are already configured for Docker:

```json
{
  "JobSearch": {
    "InteractivePrompt": false
  },
  "DataConnection": {
    "Provider": "postgres",
    "ConnectionString": "Host=postgres;Database=linkedinbot;Username=linkedinbot;Password=linkedinbot"
  },
  "Browser": {
    "Headless": true,
    "Channel": "chromium"
  }
}
```

#### 2. Build and run

```bash
docker compose up --build
```

This starts PostgreSQL + Worker. The Worker auto-applies database migrations on startup.

#### 3. Stop

```bash
docker compose down
```

### Console (optional, for local dev)

```bash
docker compose --profile console up linkedinbot --build
```

### Docker Compose Commands

| Action | Command |
|--------|---------|
| Start Worker + Postgres | `docker compose up -d --build` |
| Start Console | `docker compose --profile console up linkedinbot --build` |
| Stop services | `docker compose down` |
| View status | `docker compose ps` |
| View logs | `docker compose logs -f worker` |
| Remove containers and volumes | `docker compose down -v` |

### Environment Variable Overrides

All settings can be overridden via environment variables using `__` (double underscore) as the section separator:

```yaml
environment:
  - JobSearch__Keywords=React AND TypeScript
  - JobSearch__MaxApplicationsPerRun=10
  - OpenAI__Model=gpt-4o-mini
  - DataConnection__Provider=postgres
  - DataConnection__ConnectionString=Host=postgres;Database=linkedinbot;Username=linkedinbot;Password=linkedinbot
  - Browser__Headless=true
  - Browser__Channel=chromium
```

### Persistent Data (Volumes)

| Volume | Purpose |
|--------|---------|
| `postgres-data` | PostgreSQL database (job history) |
| `worker-userdata` | Browser session data (login persistence) |
| `worker-logs` | Daily rolling log files |

---

## 🔄 How It Works

### Application Flow

```
1. Initialize Browser (persistent Chrome context)
        │
2. Ensure LinkedIn Login (session persistence)
        │
3. ┌──► Search Jobs (configurable filters)
   │        │
   │    4. Scroll & Collect All Jobs
   │        │
   │    5. Filter Already Analyzed (via job history)
   │        │
   │    6. For Each New Job:
   │    │   ├── Analyze Compatibility with GPT-4o (≥60% threshold)
   │    │   ├── If Incompatible → Dismiss from feed
   │    │   └── If Compatible + Easy Apply:
   │    │       ├── Click "Easy Apply"
   │    │       ├── Handle Safety Reminder (if shown)
   │    │       ├── Fill Form Steps (text, select, radio, textarea)
   │    │       ├── Submit Application
   │    │       └── Save Result to History
   │    │
   │    7. Print Cycle Summary
   │    │
   └────8. Wait N Minutes → Next Cycle
```

### Console vs Worker

| Feature | Console | Worker |
|---------|---------|--------|
| Entry point | `Program.cs` (top-level) | `LinkedInBotWorker.cs` (BackgroundService) |
| Interactive prompt | Yes (pauses on unrecognized forms) | No (skips automatically) |
| Default persistence | JSON file | PostgreSQL |
| Default browser | Chrome (visible) | Chromium (headless) |
| Graceful shutdown | `Ctrl+C` | SIGTERM / Docker stop |
| DbContext scope | Single scope | Fresh scope per cycle |

### Interactive Pause (Console only)

When `InteractivePrompt` is `true` and the bot encounters an unrecognized form element, it pauses:

```
Resolve the issue manually in the browser, then:
[C] Continue processing this job
[S] Stop the bot
Choice:
```

### Graceful Shutdown

Press `Ctrl+C` (Console) or send SIGTERM (Worker/Docker) to gracefully stop the bot. It finishes the current operation, prints a session summary, and closes the browser.

---

## 🔄 CI/CD

### GitHub Actions

Two workflows automate versioning and releases:

**1. Version and Tag** (`version-tag.yml`)
- **Trigger:** Push to `main` or manual dispatch
- Uses GitVersion to calculate the semantic version
- Creates and pushes a git tag `v{version}` if it doesn't already exist

**2. Create Release** (`create-release.yml`)
- **Trigger:** After "Version and Tag" workflow completes successfully
- Creates a release branch `releases/v{version}` for minor/major bumps
- Creates a GitHub Release with auto-generated notes
- Patch-only changes are tagged but do not create a release

### Commit Message Conventions

| Prefix | Version Bump | Example |
|--------|-------------|---------|
| `major:` or `breaking:` | Major (X.0.0) | `major: redesign persistence layer` |
| `feat:` or `feature:` | Minor (0.X.0) | `feat: add SQLite provider` |
| `fix:` or `patch:` | Patch (0.0.X) | `fix: handle null selector` |
| `+semver: none` | No bump | `+semver: none update docs` |

---

## 🔍 Troubleshooting

### Common Issues

#### Playwright browser not found

**Solution:**
```bash
powershell -Command "playwright install chromium"
```

#### File lock errors during build

The bot process may be locking DLL files.

**Solution:**
```bash
powershell -Command "Get-Process LinkedinBot.Console -ErrorAction SilentlyContinue | Stop-Process -Force"
dotnet build
```

#### LinkedIn selectors changed

LinkedIn frequently updates their DOM structure. If buttons or elements are not being found:

1. Inspect the element in Chrome DevTools
2. Update the selector in `LinkedinBot.Infra/Constants/Selectors.cs`
3. Rebuild and test

#### Select/Combo box not filling

If a select dropdown times out, check the logs for the option values. The bot uses the `value` attribute (not `innerText`) for reliable selection. If the placeholder text uses a language not yet supported, add it to `IsPlaceholderOption()` in `LinkedInApplyAppService.cs`.

#### Database connection issues (Postgres/SQLite)

**Check the DataConnection section:**
```json
"DataConnection": {
  "Provider": "postgres",
  "ConnectionString": "Host=localhost;Port=5432;Database=linkedinbot;Username=linkedinbot;Password=linkedinbot"
}
```

**Common causes:**
- PostgreSQL not running (Docker: `docker compose ps`)
- Wrong hostname (`localhost` for local, `postgres` for Docker networking)
- Missing database — migrations auto-apply on startup, but the server must be reachable

**For SQLite:**
- Ensure the directory is writable
- Default file: `job-history.db` in the working directory

---

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

### Development Setup

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Make your changes
4. Build and verify (`dotnet build`)
5. Commit your changes (`git commit -m 'feat: add some AmazingFeature'`)
6. Push to the branch (`git push origin feature/AmazingFeature`)
7. Open a Pull Request

### Coding Standards

- Follow Clean Architecture layer boundaries
- Use `Selectors.cs` for all LinkedIn DOM selectors
- Use `IOptions<T>` for all configuration access
- Log at `Information` level for user-visible actions
- Log at `Warning` level for recoverable errors
- Log at `Error` level for failures that abort operations

---

## 👨‍💻 Author

Developed by **[Rodrigo Landim Carneiro](https://github.com/landim32)**

---

## 📄 License

This project is licensed under the **MIT License** — see the [LICENSE](LICENSE) file for details.

---

## 🙏 Acknowledgments

- Built with [Microsoft Playwright](https://playwright.dev/dotnet/)
- Powered by [OpenAI GPT-4o](https://openai.com/)
- Structured logging with [Serilog](https://serilog.net/)

---

## 📞 Support

- **Issues**: [GitHub Issues](https://github.com/landim32/LinkedinBot/issues)

---

**⭐ If you find this project useful, please consider giving it a star!**
