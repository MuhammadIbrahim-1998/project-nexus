using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nexus.Application.Common.Interfaces;
using Nexus.Domain.Entities;
using Nexus.Domain.Enums;
using Nexus.Infrastructure.Agents.Orchestrator;
using Nexus.Infrastructure.ExternalServices.DeepSeek;
using Nexus.Infrastructure.Hubs;

namespace Nexus.Infrastructure.Agents.Matching;

public class MatchingAgentService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MatchingAgentService> _logger;
    private readonly IHubContext<AgentStatusHub> _hubContext;
    private readonly bool _enabled;
    private readonly int _intervalMinutes;

    public MatchingAgentService(
        IServiceScopeFactory scopeFactory,
        ILogger<MatchingAgentService> logger,
        IConfiguration config,
        IHubContext<AgentStatusHub> hubContext)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _hubContext = hubContext;
        _enabled = config["MatchingAgent:Enabled"] != "false";
        _intervalMinutes = int.TryParse(config["MatchingAgent:IntervalMinutes"], out var m) ? m : 60;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_enabled)
        {
            _logger.LogInformation("Matching Agent is disabled via configuration.");
            return;
        }

        try
        {
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            while (!stoppingToken.IsCancellationRequested)
            {
                await RunOnceAsync(stoppingToken);
                await Task.Delay(TimeSpan.FromMinutes(_intervalMinutes), stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // normal on shutdown
        }
    }

    internal async Task<AgentRunResult> RunOnceAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<INexusDbContext>();
        var client = scope.ServiceProvider.GetRequiredService<DeepSeekMatchingClient>();
        var settings = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        var status = AgentRunStatus.Success;
        var matchedCount = 0;
        string? error = null;

        await _hubContext.Clients.All.SendAsync("AgentStatus", new
        {
            AgentType = "Matching",
            State = "Started",
            Message = "Matching Agent chal raha hai...",
            Timestamp = DateTime.UtcNow
        }, ct);

        try
        {
            var jobs = await db.Jobs
                .Where(j => j.MatchedScore == null && j.Description != null)
                .ToListAsync(ct);

            foreach (var job in jobs)
            {
                if (ct.IsCancellationRequested) break;

                await _hubContext.Clients.All.SendAsync("AgentStatus", new
                {
                    AgentType = "Matching",
                    State = "Progress",
                    Message = $"Matching job: {job.Title} at {job.Company}",
                    Timestamp = DateTime.UtcNow
                }, ct);

                try
                {
                    var result = await client.MatchJobAsync(
                        job.Title,
                        job.Description,
                        settings["UserProfile:Skills"] ?? "",
                        settings["UserProfile:Experience"] ?? "",
                        settings["UserProfile:PreferredRoles"] ?? "",
                        ct);

                    if (result != null)
                    {
                        job.MatchedScore = result.Score;
                        job.MatchReasoning = result.Reasoning;
                        matchedCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to match job {JobId} ({Title})", job.Id, job.Title);
                }
            }
        }
        catch (Exception ex)
        {
            status = AgentRunStatus.Failed;
            error = ex.Message;
            _logger.LogError(ex, "Matching Agent run failed");
        }

        db.AgentLogs.Add(new AgentLog
        {
            AgentType = AgentType.Matching,
            Status = status,
            RunAt = DateTime.UtcNow,
            Result = matchedCount + " job(s) matched.",
            ErrorMessage = error
        });
        await db.SaveChangesAsync(ct);

        _logger.LogInformation("Matching Agent: {Count} job(s) matched.", matchedCount);
        await _hubContext.Clients.All.SendAsync("AgentStatus", new
        {
            AgentType = "Matching",
            State = status == AgentRunStatus.Success ? "Completed" : "Failed",
            Message = $"Matching Agent complete: {matchedCount} job(s) matched.",
            Timestamp = DateTime.UtcNow
        }, ct);

        return new AgentRunResult(
            "Matching",
            status,
            $"Matching complete: {matchedCount} job(s) matched.",
            matchedCount,
            error);
    }
}