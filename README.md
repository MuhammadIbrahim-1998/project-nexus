# Project Nexus

**An agentic, multi-agent job-search dashboard built with .NET — and a portfolio piece that demonstrates the very skills it uses.**

Project Nexus is a semi-automated system that discovers remote software roles, scores them against a candidate profile, and drafts tailored application content — while keeping a human firmly in control of every final decision. It is deliberately built as a *product*, using production-grade patterns (Clean Architecture, CQRS, background agents, cloud deployment), so that the codebase itself doubles as a demonstration of Agentic AI, ML, and .NET engineering.

> **Status:** In active development. Backend-first build. See the [Roadmap](#roadmap) for current progress.

---

## Why this project exists

It serves two goals at once:

1. **A working agentic-AI application** — something real to show on a CV/GitHub, not a toy demo.
2. **A genuinely useful tool** — one that helps automate the tedious parts of a remote job search (discovery, matching, drafting), with the human making every final call.

The guiding principle: *treat the job search like a software product — and let that product be the portfolio.*

---

## Architecture

The solution follows **Clean Architecture**, with dependencies always pointing inward toward the domain:

```
Nexus.API            →  Presentation layer (controllers, DI wiring, OpenAPI)
   │
   ├── Nexus.Application     →  Use cases: CQRS commands/queries, handlers, DTOs, interfaces
   │        │
   │        └── Nexus.Domain →  Entities, enums, core business rules (no dependencies)
   │
   └── Nexus.Infrastructure  →  EF Core, DbContext, external services (implements Application interfaces)
```

- **CQRS with MediatR** separates read (queries) from write (commands), keeping controllers thin.
- The **Application layer depends only on interfaces** (e.g. `INexusDbContext`), never on Infrastructure directly — so the domain stays isolated and testable.
- **EF Core migrations** manage the database schema as versioned code.

*(An architecture diagram and UI screenshots will be added once the frontend lands.)*

---

## Tech Stack

| Layer            | Technology                                        |
|------------------|---------------------------------------------------|
| Backend          | .NET Core Web API (.NET 10)                        |
| Architecture     | Clean Architecture + CQRS / MediatR               |
| Data             | Entity Framework Core + SQL Server                 |
| Background work  | .NET hosted services / scheduled jobs             |
| Real-time UI     | SignalR (live agent status)                       |
| Frontend         | React                                             |
| AI               | Claude API (Anthropic)                            |
| Cloud            | Azure App Service / Container Apps                 |
| CI/CD            | GitHub Actions                                     |
| API testing      | Scalar (OpenAPI reference UI)                      |

---

## Features

**Available now**
- Clean Architecture solution with four layered projects
- EF Core data model (`Jobs`, `Applications`, `AgentLogs`) with relationships and migrations
- CQRS endpoints for listing and creating jobs

**Planned**
- Discovery Agent — background service that pulls in new roles
- Matching Agent — ML/embedding-based scoring against a candidate profile
- Content Generation Agent — Claude-drafted, tailored CVs and cover letters
- Data Analysis Agent — application trends and response-rate insights
- Orchestrator Agent — coordinates the agents on a schedule
- React dashboard with live agent status via SignalR
- Azure deployment with GitHub Actions CI/CD

---

## Getting Started

### Prerequisites
- [.NET SDK 10](https://dotnet.microsoft.com/download)
- SQL Server (Express is fine) + a client such as SSMS
- Node.js (for the React frontend, added later)

### Setup

```bash
# 1. Clone
git clone https://github.com/MuhammadIbrahim-1998/project-nexus.git
cd project-nexus

# 2. Configure the database connection (kept out of source control)
dotnet user-secrets init --project src/Nexus.API
dotnet user-secrets set "ConnectionStrings:Default" \
  "Server=localhost\SQLEXPRESS;Database=ProjectNexus;Trusted_Connection=True;TrustServerCertificate=True;" \
  --project src/Nexus.API

# 3. Apply migrations to create the database
dotnet ef database update -s src/Nexus.Infrastructure

# 4. Run the API
dotnet run --project src/Nexus.API
```

Then open the API reference UI in your browser at `https://localhost:<port>/scalar` to explore and test the endpoints.

> **Note:** Secrets (connection strings, the Anthropic API key) are never committed. They live in .NET User Secrets locally and in the cloud provider's secret store in production.

---

## Project Structure

```
ProjectNexus/
├── src/
│   ├── Nexus.Domain/          # Entities, enums, base types
│   ├── Nexus.Application/     # CQRS features, DTOs, interfaces, DI
│   ├── Nexus.Infrastructure/  # EF Core DbContext, migrations
│   └── Nexus.API/             # Controllers, Program.cs, OpenAPI
└── ProjectNexus.sln
```

---

## Roadmap

- [x] **Step 1 — Database design:** Clean Architecture scaffold, EF Core entities, migrations, live SQL Server database
- [ ] **Step 2 — Web API foundation:** CQRS/MediatR, first Jobs endpoints
- [ ] **Step 3 — Discovery Agent:** background service for pulling roles
- [ ] **Step 4 — React frontend:** jobs list, match scores, live agent status
- [ ] **Step 5 — Remaining agents:** Matching, Content Generation, Data Analysis, Orchestrator
- [ ] **Step 6 — Azure deployment + CI/CD**

---

## Safety & Ethics

This project is an **assistant**, not a spam bot. It is built with clear guardrails:

- No automated applying on platforms whose terms of service prohibit it.
- The final "submit" on any application is always a human action.
- AI-generated content (CVs, cover letters) is always reviewed by a human before it is sent anywhere.

---

## License

Released under the MIT License.

## Author

**Muhammad Ibrahim** — [@MuhammadIbrahim-1998](https://github.com/MuhammadIbrahim-1998)
