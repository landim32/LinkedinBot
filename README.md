# LinkedinBot - LinkedIn Easy Apply Automation

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)
![Playwright](https://img.shields.io/badge/Playwright-1.49.0-2EAD33?logo=playwright)
![OpenAI](https://img.shields.io/badge/OpenAI-GPT--4o-412991?logo=openai)
![License](https://img.shields.io/badge/License-MIT-green)

## Overview

**LinkedinBot** is an intelligent LinkedIn job application automation tool that uses **Playwright** for browser automation and **OpenAI GPT-4o** for AI-powered decision making. It automatically searches for jobs, evaluates compatibility with your resume, fills out Easy Apply forms, and submits applications — all with minimal manual intervention.

Built with **.NET 8.0** following **Clean Architecture** principles across 6 layered projects, ensuring separation of concerns, testability, and maintainability.

---

## ⚡ Quick Start

### 1. Set up your ChatGPT API Key

Copy the template and edit `appsettings.json`:

```bash
cp LinkedinBot.Console/appsettings.example.json LinkedinBot.Console/appsettings.json
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
cp LinkedinBot.Console/resume.example.md LinkedinBot.Console/resume.md
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

## 🚀 Features

- 🤖 **AI-Powered Job Analysis** — Uses GPT-4o to evaluate job compatibility against your resume with a configurable confidence threshold
- 📝 **Smart Form Filling** — Automatically fills text inputs, textareas, selects, and radio groups using AI-generated answers
- 🔍 **Advanced Job Search** — Configurable search with keywords, boolean operators, location (GeoId), distance, experience level, and remote filters
- 🔄 **Continuous Execution** — Runs in a loop with configurable intervals, automatically searching for and applying to new jobs
- 📊 **Job History Tracking** — JSON-based persistence to avoid re-processing already analyzed jobs across sessions
- 🛡️ **Safety Dialog Handling** — Automatically dismisses LinkedIn's security reminder dialogs
- ⏸️ **Interactive Pause** — Pauses on unrecognized form elements, allowing manual resolution before continuing
- 🚫 **Job Dismissal** — Automatically dismisses incompatible jobs from the feed
- 📜 **Dynamic Scrolling** — Scrolls through entire job list pages to load all lazy-loaded content
- 🔢 **Numeric Field Sanitization** — Detects input types and sanitizes numeric fields (salary, phone, etc.)
- 🌐 **Multi-Language Placeholder Detection** — Recognizes select placeholders in Portuguese, Spanish, and English
- 📋 **Detailed Logging** — Step-by-step logging of every form action with Serilog (console + file)

---

## 🛠️ Technologies Used

### Core Framework
- **.NET 8.0** — Target framework with implicit usings and nullable reference types

### Browser Automation
- **Microsoft.Playwright 1.49.0** — Browser automation with persistent Chrome context for session persistence

### AI Integration
- **OpenAI SDK 2.1.0** — GPT-4o integration for job compatibility analysis and intelligent form filling

### Logging
- **Serilog** — Structured logging with console and daily-rolling file sinks

### Dependency Injection & Configuration
- **Microsoft.Extensions.Hosting 8.0.1** — Host builder with DI container and configuration
- **Microsoft.Extensions.DependencyInjection** — Service registration and lifetime management
- **Microsoft.Extensions.Options** — Strongly-typed settings binding from `appsettings.json`

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
├── LinkedinBot.Infra.Interfaces/       # Infrastructure contracts (→ Domain, DTO)
│   └── Services/
│       ├── IBrowserService.cs          # Playwright browser lifecycle
│       ├── ILinkedInAuthService.cs     # LinkedIn session management
│       ├── ILinkedInSearchService.cs   # Job collection & dismissal
│       ├── ILinkedInApplyService.cs    # Easy Apply form automation
│       └── IJobHistoryService.cs       # Job history persistence
│
├── LinkedinBot.Infra/                  # Implementations (→ Infra.Interfaces, Domain, DTO)
│   ├── Services/
│   │   ├── BrowserService.cs           # Playwright initialization
│   │   ├── LinkedInAuthService.cs      # Login with session persistence
│   │   ├── LinkedInSearchService.cs    # Search URL builder & job scraping
│   │   ├── LinkedInApplyService.cs     # Form filling & submission
│   │   ├── ChatGptService.cs           # OpenAI API integration
│   │   └── JobHistoryService.cs        # JSON file persistence
│   └── Constants/
│       └── Selectors.cs                # Centralized LinkedIn DOM selectors
│
├── LinkedinBot.Application/            # DI wiring (→ all projects)
│   └── Initializer.cs                  # Service registration & config binding
│
├── LinkedinBot.Console/                # Entry point (→ Application, Infra.Interfaces, Domain, DTO)
│   ├── Program.cs                      # Main orchestration loop
│   ├── appsettings.json                # Runtime config (gitignored)
│   ├── appsettings.example.json        # Configuration template
│   ├── resume.md                       # Your resume in Markdown (gitignored)
│   └── resume.example.md              # Resume template
│
├── .gitignore
├── LICENSE                             # MIT License
└── README.md
```

### Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    LinkedinBot.Console                       │
│                  (Program.cs — Entry Point)                  │
└──────────────────────────┬──────────────────────────────────┘
                           │
┌──────────────────────────▼──────────────────────────────────┐
│                  LinkedinBot.Application                     │
│              (Initializer.cs — DI Wiring)                    │
└──────────────────────────┬──────────────────────────────────┘
                           │
          ┌────────────────┼────────────────┐
          ▼                ▼                ▼
┌─────────────────┐ ┌─────────────┐ ┌──────────────────────┐
│ LinkedinBot.Infra│ │  .Domain    │ │ .Infra.Interfaces    │
│ (Implementations)│ │ (Services)  │ │ (Contracts + IPage)  │
└────────┬────────┘ └──────┬──────┘ └──────────┬───────────┘
         │                 │                    │
         └────────────────►├◄───────────────────┘
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
cp LinkedinBot.Console/appsettings.example.json LinkedinBot.Console/appsettings.json
cp LinkedinBot.Console/resume.example.md LinkedinBot.Console/resume.md
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
    "HistoryFilePath": "./job-history.json",
    "SalaryExpectation": 15000,
    "MaxFormSteps": 20
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
| **Browser** | `Headless` | Run browser without UI | `false` |
| **Browser** | `SlowMo` | Delay between actions (ms) | `500` |
| **Browser** | `Channel` | Browser channel | `chrome` |
| **Resume** | `MarkdownPath` | Path to your resume.md | `./resume.md` |

⚠️ **IMPORTANT**:
- Never commit `appsettings.json` with real credentials — it is gitignored
- Never commit `resume.md` with personal data — it is gitignored
- Only `.example` files are version controlled

---

## 🔧 Setup

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Google Chrome](https://www.google.com/chrome/)
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
powershell -Command "dotnet tool install --global Microsoft.Playwright.CLI"
powershell -Command "playwright install chromium"
```

#### 3. Configure the application

```bash
cp LinkedinBot.Console/appsettings.example.json LinkedinBot.Console/appsettings.json
cp LinkedinBot.Console/resume.example.md LinkedinBot.Console/resume.md
```

Edit both files with your credentials and resume (see [Environment Configuration](#️-environment-configuration)).

#### 4. Build

```bash
dotnet build
```

#### 5. Run

```bash
dotnet run --project LinkedinBot.Console
```

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
   │    5. Filter Already Analyzed (via job-history.json)
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

### Interactive Pause

When the bot encounters an unrecognized form element or reaches the maximum form steps, it pauses and prompts:

```
Resolve the issue manually in the browser, then:
[C] Continue processing this job
[S] Stop the bot
Choice:
```

This allows you to manually fix any form issues while the bot waits, then resume automation.

### Graceful Shutdown

Press `Ctrl+C` at any time to gracefully stop the bot. It will finish the current operation, print a session summary, and close the browser.

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

If a select dropdown times out, check the logs for the option values. The bot uses the `value` attribute (not `innerText`) for reliable selection. If the placeholder text uses a language not yet supported, add it to `IsPlaceholderOption()` in `LinkedInApplyService.cs`.

---

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

### Development Setup

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Make your changes
4. Build and verify (`dotnet build`)
5. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
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
