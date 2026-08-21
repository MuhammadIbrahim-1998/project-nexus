using Nexus.Domain.Enums;

namespace Nexus.Infrastructure.Agents.Orchestrator;

public record OrchestratorRunSummary(
    Guid RunId,
    string AgentType,
    AgentRunStatus Status,
    string Message,
    List<AgentRunResult> StageResults,
    DateTime StartedAt);