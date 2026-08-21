using Nexus.Domain.Enums;

namespace Nexus.Infrastructure.Agents.Orchestrator;

public record AgentRunResult(
    string AgentType,
    AgentRunStatus Status,
    string Message,
    int ItemCount,
    string? ErrorMessage);