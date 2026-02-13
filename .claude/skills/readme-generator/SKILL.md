---
name: readme-generator
description: Generates a comprehensive, standardized README.md for any project. Use when the user wants to create or regenerate a README file following the project's documentation standard.
allowed-tools: Read, Grep, Glob, Bash, Write, Task
user-invocable: true
---

# Generate Standardized README.md

You are a README.md generator that creates comprehensive, well-structured project documentation. Your task is to analyze the project and generate a complete README following a strict template.

## Input

The user may provide additional context or a project path as argument: `$ARGUMENTS`

If no arguments are provided, analyze the current project directory.

## Instructions

### Phase 1 — Project Discovery

Analyze the project to gather all necessary information:

1. **Identify the project type**: Check for project files to determine the technology stack:
   - `.csproj` / `.sln` → .NET
   - `package.json` → Node.js / React / Angular
   - `pom.xml` / `build.gradle` → Java
   - `requirements.txt` / `pyproject.toml` / `setup.py` → Python
   - `go.mod` → Go
   - `Cargo.toml` → Rust
   - `Gemfile` → Ruby
   - Other config files as needed

2. **Read project configuration files**: Read the main project config to extract:
   - Project name and version
   - Description (if available)
   - Dependencies and their versions
   - Build/run scripts
   - License

3. **Analyze the directory structure**: Use `ls` and `Glob` to understand the folder structure, identify key directories (source, tests, docs, config, docker, CI/CD).

4. **Check for Docker support**: Look for `Dockerfile`, `docker-compose.yml`, `.dockerignore`.

5. **Check for CI/CD**: Look for `.github/workflows/`, `.gitlab-ci.yml`, `Jenkinsfile`, `azure-pipelines.yml`, `bitbucket-pipelines.yml`.

6. **Check for existing badges**: Look at existing README (if any), SonarCloud, NuGet, NPM, or other badge sources.

7. **Check for environment configuration**: Look for `.env.example`, `appsettings.*.json`, `config/` directories.

8. **Check for tests**: Identify test frameworks and test directory structure.

9. **Check for related ecosystem projects**: Look for references to sibling repositories, packages, or monorepo structure.

### Phase 2 — Generate README

Generate the README.md following the exact template structure below. **Only include sections that are relevant to the project.** If the project has no Docker setup, skip the Docker section. If no CI/CD, skip that section.

### Phase 3 — Save the File

Save the generated README to `README.md` in the project root (or the path specified by the user).

## README Template

The generated README MUST follow this structure and formatting standard:

```markdown
# <Project Name> - <Short Tagline>

![<Framework>](https://img.shields.io/badge/<Framework>-<Version>-blue)
![License](https://img.shields.io/badge/License-<License>-green)
<!-- Add relevant badges: SonarCloud, NuGet, NPM, build status, coverage, etc. -->

## Overview

**<Project Name>** is <one paragraph description of what the project does, who it's for, and what problems it solves>. Built using **<main technologies>**.

<If part of an ecosystem, describe the relationship with other projects here.>

<Brief mention of architecture approach if relevant.>

---

## 🚀 Features

- 🔐 **Feature 1** - Brief description
- 🔑 **Feature 2** - Brief description
- 🔄 **Feature 3** - Brief description
<!-- List all major features with appropriate emoji and bold title -->

---

## 🛠️ Technologies Used

### Core Framework
- **<Framework>** - Brief description

### Database
- **<Database>** - Brief description
<!-- Only if applicable -->

### Security
- **<Security tech>** - Brief description
<!-- Only if applicable -->

### Additional Libraries
- **<Library>** - Brief description

### Testing
- **<Test framework>** - Brief description

### DevOps
- **<DevOps tool>** - Brief description
<!-- Only if applicable -->

---

## 📁 Project Structure

\`\`\`
<ProjectRoot>/
├── <dir1>/                  # Description
│   ├── <subdir>/            # Description
│   └── <file>               # Description
├── <dir2>/                  # Description
├── <config-file>            # Description
└── README.md                # This file
\`\`\`

<!-- If part of an ecosystem, add: -->

### Ecosystem

| Project | Type | Package | Description |
|---------|------|---------|-------------|
| **[Project1](url)** | Type | Badge | Description |

#### Dependency graph

\`\`\`
<ASCII art dependency graph>
\`\`\`

---

## ⚙️ Environment Configuration

Before running the application, you need to configure the environment variables:

### 1. Copy the environment template

\`\`\`bash
cp .env.example .env
\`\`\`

### 2. Edit the \`.env\` file

\`\`\`bash
# Variable descriptions with example values
VARIABLE_NAME=example_value
\`\`\`

⚠️ **IMPORTANT**:
- Never commit the \`.env\` file with real credentials
- Only the \`.env.example\` should be version controlled
- Change all default passwords and secrets before deployment

---

## 🐳 Docker Setup

### Quick Start with Docker Compose

#### 1. Prerequisites

\`\`\`bash
# Any required network or pre-setup commands
\`\`\`

#### 2. Build and Start Services

\`\`\`bash
docker-compose up -d --build
\`\`\`

#### 3. Verify Deployment

\`\`\`bash
docker-compose ps
docker-compose logs -f
\`\`\`

### Accessing the Application

| Service | URL |
|---------|-----|
| **Service Name** | http://localhost:PORT |

### Docker Compose Commands

| Action | Command |
|--------|---------|
| Start services | \`docker-compose up -d\` |
| Start with rebuild | \`docker-compose up -d --build\` |
| Stop services | \`docker-compose stop\` |
| View status | \`docker-compose ps\` |
| View logs | \`docker-compose logs -f\` |
| Remove containers | \`docker-compose down\` |
| Remove containers and volumes (⚠️) | \`docker-compose down -v\` |

---

## 🔧 Manual Setup (Without Docker)

### Prerequisites
- <Prerequisite 1>
- <Prerequisite 2>

### Setup Steps

#### 1. <Step Title>

\`\`\`bash
<commands>
\`\`\`

#### 2. <Step Title>

\`\`\`bash
<commands>
\`\`\`

---

## 🧪 Testing

### Running Tests

**All Tests:**
\`\`\`bash
<test command>
\`\`\`

**With Coverage:**
\`\`\`bash
<coverage command>
\`\`\`

### Test Structure

\`\`\`
<TestDir>/
├── <category1>/         # Description
├── <category2>/         # Description
└── <category3>/         # Description
\`\`\`

---

## 📚 API Documentation

<!-- Only for API projects. Include authentication flow, endpoint summary, key examples. -->

### Authentication Flow

\`\`\`
1. Step 1 → 2. Step 2 → 3. Step 3
\`\`\`

### Key Endpoints

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| POST | \`/endpoint\` | Description | No |
| GET | \`/endpoint/{id}\` | Description | Yes |

---

## 🔒 Security Features

### <Security Category>
- **Feature** - Description

---

## 💾 Backup and Restore

<!-- Only for projects with databases -->

### Backup

\`\`\`bash
<backup command>
\`\`\`

### Restore

\`\`\`bash
<restore command>
\`\`\`

---

## 🔍 Troubleshooting

### Common Issues

#### <Issue Title>

**Check:**
\`\`\`bash
<diagnostic command>
\`\`\`

**Common causes:**
- Cause 1
- Cause 2

**Solutions:**
- Solution 1
- Solution 2

---

## 📦 Integration

### Using <Project> in Your Application

#### Option 1: <Integration Method>

\`\`\`<language>
// Example code
\`\`\`

---

## 🚀 Deployment

### Development Environment

\`\`\`bash
<dev command>
\`\`\`

### Production Environment

\`\`\`bash
<prod command>
\`\`\`

### Cloud Deployment

<!-- Only if relevant. Include examples for major cloud providers. -->

---

## 🔄 CI/CD

### <CI/CD Platform>

**Workflow triggers:**
- Trigger 1
- Trigger 2

**Workflow steps:**
1. Step 1
2. Step 2

---

## 🧩 Roadmap

### Planned Features

- [ ] **Feature 1** - Description
- [ ] **Feature 2** - Description

---

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

### Development Setup

1. Fork the repository
2. Create a feature branch (\`git checkout -b feature/AmazingFeature\`)
3. Make your changes
4. Run tests (\`<test command>\`)
5. Commit your changes (\`git commit -m 'Add some AmazingFeature'\`)
6. Push to the branch (\`git push origin feature/AmazingFeature\`)
7. Open a Pull Request

### Coding Standards

- <Standard 1>
- <Standard 2>

---

## 👨‍💻 Author

Developed by **[Author Name](GitHub URL)**

---

## 📄 License

This project is licensed under the **<License Name>** - see the LICENSE file for details.

---

## 🙏 Acknowledgments

- Built with [Technology 1](URL)
- Powered by [Technology 2](URL)

---

## 📞 Support

- **Issues**: [GitHub Issues](<issues URL>)
- **Discussions**: [GitHub Discussions](<discussions URL>)

---

**⭐ If you find this project useful, please consider giving it a star!**
```

## Critical Rules

1. **Only include relevant sections**: If the project has no Docker, skip the Docker section. If no database, skip Backup/Restore. If not an API, skip API Documentation. The template above shows ALL possible sections — only use the ones that apply.

2. **Accurate information only**: Every piece of information in the README must come from actual project files. Do NOT invent features, dependencies, or configurations that don't exist.

3. **Realistic badge URLs**: Only add badges for services that are actually configured (SonarCloud, NuGet, NPM, etc.). Check for existing badge configurations in CI/CD files or existing README.

4. **Complete project structure**: Show the actual directory tree, not a made-up one. Use `ls` and `Glob` to verify what exists.

5. **Correct commands**: All build, test, run, and Docker commands must be verified against actual project configuration files (`package.json` scripts, `.csproj` settings, `Makefile`, etc.).

6. **Preserve existing content**: If the user has an existing README with custom content (like a specific roadmap or acknowledgments), ask whether to preserve it or regenerate.

7. **Emoji consistency**: Use the emoji style shown in the template for section headers. Each feature in the Features list should have a contextually appropriate emoji.

8. **Horizontal rules**: Use `---` between major sections for visual separation.

9. **Tables for structured data**: Use markdown tables for Docker commands, endpoints, ecosystem packages, and other structured data.

10. **Code blocks with language hints**: Always specify the language in fenced code blocks (```bash, ```json, ```csharp, ```javascript, etc.).

11. **Environment variables**: Never include real secrets or passwords in examples. Always use placeholder values like `your_secure_password_here_change_this`.

12. **Git remote detection**: Try to detect the GitHub/GitLab repository URL from `.git/config` to generate correct links for Issues, Discussions, and related projects.

## After Generating

After creating the README, inform the user:
- The file path where the README was saved
- Which sections were included and which were skipped (and why)
- Any information that could not be auto-detected and may need manual review
