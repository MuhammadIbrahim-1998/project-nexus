using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Nexus.Domain.Enums;
using Nexus.Infrastructure.Agents.ContentGeneration;
using Nexus.Infrastructure.Agents.Discovery;
using Nexus.Infrastructure.Agents.Matching;
using Nexus.Infrastructure.Hubs;

namespace Nexus.Infrastructure.Agents.Orchestrator;

public class NexusOrchestratorService
{
    private readonly DiscoveryAgentService _discoveryAgent;
    private readonly MatchingAgentService _matchingAgent;
    private readonly ContentGenerationAgentService _contentGenerationAgent;
    private readonly IHubContext<AgentStatusHub> _hubContext;
    private readonly ILogger<NexusOrchestratorService> _logger;
    private static readonly SemaphoreSlim _runGate = new(1, 1);

    public NexusOrchestratorService(
        DiscoveryAgentService discoveryAgent,
        MatchingAgentService matchingAgent,
        ContentGenerationAgentService contentGenerationAgent,
        IHubContext<AgentStatusHub> hubContext,
        ILogger<NexusOrchestratorService> logger)
    {
        _discoveryAgent = discoveryAgent;
        _matchingAgent = matchingAgent;
        _contentGenerationAgent = contentGenerationAgent;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task<OrchestratorRunSummary> RunFullPipelineAsync(CancellationToken ct)
    {
        if (!await _runGate.WaitAsync(0, ct))
        {
            var conflictMessage = "A pipeline run is already in progress. Please wait for it to complete before starting another.";
            await BroadcastAsync("Orchestrator", "Failed", conflictMessage, ct);
            return new OrchestratorRunSummary(
                Guid.NewGuid(),
                "Pipeline",
                AgentRunStatus.Failed,
                conflictMessage,
                new List<AgentRunResult>(),
                DateTime.UtcNow);
        }

        try
        {
            var runId = Guid.NewGuid();
            var startedAt = DateTime.UtcNow;
            var results = new List<AgentRunResult>();

            await BroadcastAsync("Orchestrator", "Started", "Starting Discovery phase...", ct);

            var discoveryResult = await _discoveryAgent.RunOnceAsync(ct);
            results.Add(discoveryResult);
            await BroadcastAsync("Orchestrator", "Progress", "Discovery complete, starting Matching phase...", ct);

            var matchingResult = await _matchingAgent.RunOnceAsync(ct);
            results.Add(matchingResult);
            await BroadcastAsync("Orchestrator", "Progress", "Matching complete, starting Content Generation phase...", ct);

            var contentResult = await _contentGenerationAgent.RunOnceAsync(ct);
            results.Add(contentResult);

            var overallStatus = results.All(r => r.Status == AgentRunStatus.Success)
                ? AgentRunStatus.Success
                : AgentRunStatus.Partial;

            await BroadcastAsync("Orchestrator", overallStatus == AgentRunStatus.Success ? "Completed" : "Partial", "Pipeline complete.", ct);

            return new OrchestratorRunSummary(runId, "Pipeline", overallStatus, "Pipeline complete.", results, startedAt);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            await BroadcastAsync("Orchestrator", "Failed", "Pipeline cancelled.", CancellationToken.None);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Orchestrator pipeline run failed");
            await BroadcastAsync("Orchestrator", "Failed", $"Pipeline failed: {ex.Message}", CancellationToken.None);
            throw;
        }
        finally
        {
            _runGate.Release();
        }
    }

    private async Task BroadcastAsync(string agentType, string state, string message, CancellationToken ct)
    {
        await _hubContext.Clients.All.SendAsync("AgentStatus", new
        {
            AgentType = agentType,
            State = state,
            Message = message,
            Timestamp = DateTime.UtcNow
        }, ct);
    }
}
