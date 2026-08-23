using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nexus.Application.Common.Interfaces;
using Nexus.Application.Features.Jobs.Commands.CreateJob;
using Nexus.Domain.Entities;
using Nexus.Domain.Enums;
using Nexus.Infrastructure.Agents.Orchestrator;
using Nexus.Infrastructure.Hubs;
namespace Nexus.Infrastructure.Agents.Discovery;
public class DiscoveryAgentService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DiscoveryAgentService> _logger;
    private readonly IHubContext<AgentStatusHub> _hubContext;
    private readonly bool _enabled;
    private readonly int _intervalMinutes;
    public DiscoveryAgentService(
        IServiceScopeFactory scopeFactory,
        ILogger<DiscoveryAgentService> logger,
        IConfiguration config,
        IHubContext<AgentStatusHub> hubContext)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _hubContext = hubContext;
        _enabled = config["DiscoveryAgent:Enabled"] != "false";
        _intervalMinutes = int.TryParse(config["DiscoveryAgent:IntervalMinutes"], out var m) ? m : 360;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_enabled)
        {
            _logger.LogInformation("Discovery Agent is disabled via configuration.");
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
        }
    }
    internal async Task<AgentRunResult> RunOnceAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var source = scope.ServiceProvider.GetRequiredService<IJobDiscoverySource>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var db = scope.ServiceProvider.GetRequiredService<INexusDbContext>();
        var status = AgentRunStatus.Success;
        var addedCount = 0;
        string? error = null;
        await _hubContext.Clients.All.SendAsync("AgentStatus", new
        {
            AgentType = "Discovery",
            State = "Started",
            Message = "Discovery Agent chal raha hai...",
            Timestamp = DateTime.UtcNow
        }, ct);
        try
        {
            var discovered = await source.DiscoverAsync(ct);
            foreach (var job in discovered)
            {
                bool exists = await db.Jobs
                    .AnyAsync(j => j.Title == job.Title && j.Company == job.Company, ct);
                if (exists) continue;
                await mediator.Send(new CreateJobCommand
                {
                    Title = job.Title,
                    Company = job.Company,
                    Description = job.Description,
                    Source = job.Source,
                    SourceUrl = job.SourceUrl,
                    Url = job.Url,
                    Location = job.Location,
                    IsRemote = job.IsRemote,
                    SalaryInfo = job.SalaryInfo
                }, ct);
                addedCount++;
                await _hubContext.Clients.All.SendAsync("AgentStatus", new
                {
                    AgentType = "Discovery",
                    State = "Progress",
                    Message = $"Naya job mila: {job.Title} at {job.Company}",
                    Timestamp = DateTime.UtcNow
                }, ct);
            }
        }
        catch (Exception ex)
        {
            status = AgentRunStatus.Failed;
            error = ex.Message;
            _logger.LogError(ex, "Discovery Agent run failed");
        }
        db.AgentLogs.Add(new AgentLog
        {
            AgentType = AgentType.Discovery,
            Status = status,
            RunAt = DateTime.UtcNow,
            Result = addedCount + " job(s) discovered.",
            ErrorMessage = error
        });
        await db.SaveChangesAsync(ct);
        _logger.LogInformation("Discovery Agent: {Count} new job(s) added.", addedCount);
        await _hubContext.Clients.All.SendAsync("AgentStatus", new
        {
            AgentType = "Discovery",
            State = status == AgentRunStatus.Success ? "Completed" : "Failed",
            Message = $"Discovery Agent complete: {addedCount} naye job(s) mile.",
            Timestamp = DateTime.UtcNow
        }, ct);

        return new AgentRunResult(
            "Discovery",
            status,
            $"Discovery complete: {addedCount} job(s) discovered.",
            addedCount,
            error);
    }
}
