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

namespace Nexus.Infrastructure.Agents.ContentGeneration;

public class ContentGenerationAgentService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ContentGenerationAgentService> _logger;
    private readonly IHubContext<AgentStatusHub> _hubContext;
    private readonly bool _enabled;
    private readonly int _intervalMinutes;
    private readonly int _minMatchScoreThreshold;

    public ContentGenerationAgentService(
        IServiceScopeFactory scopeFactory,
        ILogger<ContentGenerationAgentService> logger,
        IConfiguration config,
        IHubContext<AgentStatusHub> hubContext)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _hubContext = hubContext;
        _enabled = config["ContentGenerationAgent:Enabled"] != "false";
        _intervalMinutes = int.TryParse(config["ContentGenerationAgent:IntervalMinutes"], out var m) ? m : 90;
        _minMatchScoreThreshold = int.TryParse(config["ContentGenerationAgent:MinMatchScoreThreshold"], out var t) ? t : 70;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_enabled)
        {
            _logger.LogInformation("Content Generation Agent is disabled via configuration.");
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
        var client = scope.ServiceProvider.GetRequiredService<DeepSeekContentClient>();
        var settings = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        var status = AgentRunStatus.Success;
        var generatedCount = 0;
        string? error = null;

        await _hubContext.Clients.All.SendAsync("AgentStatus", new
        {
            AgentType = "ContentGeneration",
            State = "Started",
            Message = "Content Generation Agent chal raha hai...",
            Timestamp = DateTime.UtcNow
        }, ct);

        try
        {
            var jobs = await db.Jobs
                .Where(j => j.MatchedScore >= _minMatchScoreThreshold && j.GeneratedContent == null)
                .ToListAsync(ct);

            foreach (var job in jobs)
            {
                if (ct.IsCancellationRequested) break;

                await _hubContext.Clients.All.SendAsync("AgentStatus", new
                {
                    AgentType = "ContentGeneration",
                    State = "Progress",
                    Message = $"Generating content for job: {job.Title} at {job.Company}",
                    Timestamp = DateTime.UtcNow
                }, ct);

                try
                {
                    var result = await client.GenerateContentAsync(
                        job.Title,
                        job.Description,
                        settings["UserProfile:Skills"] ?? "",
                        settings["UserProfile:Experience"] ?? "",
                        settings["UserProfile:PreferredRoles"] ?? "",
                        ct);

                    if (!string.IsNullOrWhiteSpace(result))
                    {
                        job.GeneratedContent = result;
                        job.ContentGeneratedAt = DateTime.UtcNow;
                        generatedCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to generate content for job {JobId} ({Title})", job.Id, job.Title);
                }
            }
        }
        catch (Exception ex)
        {
            status = AgentRunStatus.Failed;
            error = ex.Message;
            _logger.LogError(ex, "Content Generation Agent run failed");
        }

        db.AgentLogs.Add(new AgentLog
        {
            AgentType = AgentType.ContentGeneration,
            Status = status,
            RunAt = DateTime.UtcNow,
            Result = generatedCount + " job(s) content generated.",
            ErrorMessage = error
        });
        await db.SaveChangesAsync(ct);

        _logger.LogInformation("Content Generation Agent: {Count} job(s) content generated.", generatedCount);
        await _hubContext.Clients.All.SendAsync("AgentStatus", new
        {
            AgentType = "ContentGeneration",
            State = status == AgentRunStatus.Success ? "Completed" : "Failed",
            Message = $"Content Generation Agent complete: {generatedCount} job(s) content generated.",
            Timestamp = DateTime.UtcNow
        }, ct);

        return new AgentRunResult(
            "ContentGeneration",
            status,
            $"Content Generation complete: {generatedCount} job(s) content generated.",
            generatedCount,
            error);
    }
}