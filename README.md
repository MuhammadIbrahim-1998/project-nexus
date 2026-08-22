# Project Nexus — Multi-Agent Job Search Dashboard

A portfolio project that demonstrates **Agentic AI + .NET** skills end-to-end, while doubling as a genuinely useful tool for a **semi-automated search/matching workflow for remote USD .NET jobs**. Four background agents discover, score, and draft tailored content for relevant roles — with a human always in control of the final decision.

> **Status:** Active development. Backend + agents complete. React dashboard in progress. Azure deployment planned (see [Roadmap](#roadmap)).

---

## Tech Stack

| Layer            | Technology                                            |
|------------------|-------------------------------------------------------|
| Backend          | .NET 10 Web API                                       |
| Architecture     | Clean Architecture + CQRS / MediatR                   |
| Data             | Entity Framework Core + SQL Server                    |
| AI               | Claude API (Anthropic) — via `IClaudeClient`          |
| AI               | DeepSeek API — matching "judge" + content generation  |
| Real-time UI     | SignalR (`AgentStatusHub`)                            |
| Frontend         | React + Vite + Tailwind CSS v4                        |
| API testing      | Scalar (OpenAPI reference UI)                         |
| Cloud            | Azure — **planned deployment** (App Service / Container Apps) |

---

## Architecture

Clean Architecture solution with dependencies always pointing inward. The solution is split into four .NET projects plus a React client:

```
ProjectNexus/
├── src/
│   ├── Nexus.Domain/          # Entities, enums, base types (no dependencies)
│   ├── Nexus.Application/     # CQRS commands/queries, handlers, DTOs, interfaces
│   ├── Nexus.Infrastructure/  # EF Core DbContext, migrations, agents, external services, SignalR hub
│   └── Nexus.API/             # Controllers, DI wiring, Program.cs, OpenAPI/Scalar
├── client/                    # React + Vite + Tailwind CSS v4 frontend
└── ProjectNexus.slnx
```

- **CQRS with MediatR** keeps controllers thin — reads (queries) and writes (commands) are separated.
- The **Application layer depends only on interfaces** (e.g. `INexusDbContext`, `IClaudeClient`) so the domain stays isolated and testable.
- **EF Core migrations** manage the database schema as versioned code.

---

## Agents

All agents run as .NET **BackgroundServices** and log every run to `AgentLogs`. They broadcast live progress over SignalR.

### 1. Discovery Agent
- `DiscoveryAgentService` — a `BackgroundService` that runs on a configurable interval (`DiscoveryAgent:IntervalMinutes`, default 360).
- Discovers jobs through an `IJobDiscoverySource` (currently `ClaudeJobDiscoverySource`, which asks Claude to generate sample remote .NET listings, or `DummyJobDiscoverySource` for offline dev).
- **Duplicate-checks** each job by `Title + Company` before inserting, so re-runs never create duplicates.

### 2. Matching Agent
- `MatchingAgentService` — a `BackgroundService` (default interval 60 min) that picks up unscored jobs (`MatchedScore == null`).
- Uses `DeepSeekMatchingClient` to call the **DeepSeek chat completions API as a "judge"**, which returns a JSON `{ "score": 0-100, "reasoning": "..." }` based on the job vs. the user's `UserProfile` (skills, experience, preferred roles).
- Stores the score and reasoning directly on the `Job`.

### 3. Content Generation Agent
- `ContentGenerationAgentService` — a `BackgroundService` (default interval 90 min).
- Only processes jobs that scored at or above `ContentGenerationAgent:MinMatchScoreThreshold` (default 70) and have no generated content yet.
- Uses `DeepSeekContentClient` to generate **tailored CV bullet points + a cover-letter opening paragraph** per job, storing the result in `GeneratedContent`.

### 4. Orchestrator Agent
- `NexusOrchestratorService` — a coordinating service exposed via an **on-demand endpoint**.
- Runs all three agents **sequentially**: Discovery → Matching → Content Generation.
- Uses a `SemaphoreSlim` gate to reject concurrent pipeline runs.
- Broadcasts each phase transition (Started / Progress / Completed / Failed) over SignalR using a single **"Orchestrator"** channel.

---

## Real-time Updates

SignalR hub `AgentStatusHub` is mapped at **`/hubs/agent-status`**. Every agent (and the orchestrator) pushes `AgentStatus` events with:

```json
{
  "agentType": "Discovery | Matching | ContentGeneration | Orchestrator",
  "state": "Started | Progress | Completed | Failed | Partial",
  "message": "Human-readable status",
  "timestamp": "2026-08-21T..."
}
```

The React client connects via `@microsoft/signalr` to render live pipeline progress.

---

## API Endpoints

| Method | Endpoint                               | Description                                        |
|--------|----------------------------------------|----------------------------------------------------|
| GET    | `/api/jobs`                            | List all jobs (CQRS query)                         |
| POST   | `/api/jobs`                            | Create a job (CQRS command)                        |
| GET    | `/api/analytics/dashboard-stats`       | Dashboard stats (totals, recent agent runs)        |
| POST   | `/api/orchestrator/run-full-cycle`     | Start the full agent pipeline (fire-and-forget, returns 202 Accepted with a `runId`) |
| GET    | `/scalar`                              | Scalar OpenAPI reference UI (development only)     |
| WS     | `/hubs/agent-status`                   | SignalR hub for live agent status broadcasts       |

---

## Getting Started

### Prerequisites
- [.NET SDK 10](https://dotnet.microsoft.com/download)
- SQL Server (Express is fine) + a client such as SSMS
- Node.js (18+) for the React frontend

### Backend setup

```bash
# 1. Restore packages
dotnet restore

# 2. Configure secrets locally (never committed!)
dotnet user-secrets init --project src/Nexus.API
dotnet user-secrets set "ConnectionStrings:Default" "Server=localhost\SQLEXPRESS;Database=ProjectNexus;Trusted_Connection=True;TrustServerCertificate=True;" --project src/Nexus.API
dotnet user-secrets set "DeepSeek:ApiKey" "your-deepseek-api-key" --project src/Nexus.API
dotnet user-secrets set "Anthropic:ApiKey" "your-anthropic-api-key" --project src/Nexus.API

# 3. Apply migrations to create the database
dotnet ef database update -s src/Nexus.Infrastructure

# 4. Run the API
dotnet run --project src/Nexus.API
```

Open the Scalar API reference at `https://localhost:<port>/scalar`.

### Frontend setup

```bash
# From the repo root — install Tailwind v4 workspace dependencies
npm install

# From the client/ folder — install React/Vite/SignalR dependencies
cd client
npm install
npm run dev
```

The Vite dev server runs at `http://localhost:5173` and is allowed by the API's CORS policy (`AllowReactApp`).

### Docker (frontend)

The frontend can also be built and served as a production Docker container (multi-stage build: `node:22-alpine` builds the Vite `dist`, then `nginx:alpine` serves it on port 80). The nginx config includes an SPA fallback, so refreshing on any of the four routes (`/`, `/jobs`, `/agents`, `/analytics`) never returns a 404.

```bash
# Build the image from the repo root
docker build -t nexus-frontend ./client

# Run it, mapping host port 3000 to container port 80
docker run -p 3000:80 nexus-frontend
```

Open `http://localhost:3000` — the Dashboard is the default route, and the sidebar navigates to Jobs, Agents, and Analytics.

> **Note on secrets:** Secrets (API keys, connection strings) are **never committed**. They live in .NET User Secrets locally (`dotnet user-secrets`), are read from configuration at runtime, and will live in the cloud provider's secret store in production. `appsettings.json` contains only non-sensitive defaults (logging, agent intervals, user profile); `appsettings.Development.json` contains dev-only overrides. Never put API keys in `appsettings*.json`.

---

## Roadmap

- [x] **Step 1 —** Clean Architecture scaffold, EF Core entities, initial migration
- [x] **Step 2 —** CQRS/MediatR setup, Jobs endpoints (GetAll + Create), Scalar UI
- [x] **Step 3 —** Discovery Agent (BackgroundService) with duplicate-check + agent logging
- [x] **Step 4 —** Matching Agent (DeepSeek judge: score + reasoning)
- [x] **Step 5 —** Content Generation Agent (DeepSeek: tailored CV/cover letter content)
- [x] **Step 6 —** Orchestrator Agent (sequential pipeline + SignalR progress) + Data Analysis endpoint
- [ ] **Step 7 —** React dashboard with jobs list, match scores, and live agent status
- [ ] **Step 8 —** Azure deployment (App Service / Container Apps) + CI/CD — **planned, not yet deployed**

---

## Safety & Ethics

This project is an **assistant**, not a spam bot:

- No automated applying on platforms whose terms of service prohibit it.
- The final "submit" on any application is always a human action.
- AI-generated content (CVs, cover letters) is always reviewed by a human before it is sent anywhere.

---

## Author

**Muhammad Ibrahim** — [@MuhammadIbrahim-1998](https://github.com/MuhammadIbrahim-1998)

---

## License

This project is © 2026 Muhammad Ibrahim. All rights reserved — see [LICENSE](LICENSE) for details. Code is shared publicly for portfolio review purposes.
