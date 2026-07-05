using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nexus.Application.Common.Interfaces;
using Nexus.Application.Features.Jobs.Commands.CreateJob;
using Nexus.Domain.Entities;
using Nexus.Domain.Enums;

namespace Nexus.Infrastructure.Agents.Discovery;

public class DiscoveryAgentService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DiscoveryAgentService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(30);

    public DiscoveryAgentService(IServiceScopeFactory scopeFactory, ILogger<DiscoveryAgentService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await RunOnceAsync(stoppingToken);
            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var source = scope.ServiceProvider.GetRequiredService<IJobDiscoverySource>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var db = scope.ServiceProvider.GetRequiredService<INexusDbContext>();

        var status = AgentRunStatus.Success;
        var addedCount = 0;
        string? error = null;

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
                    Url = job.Url,
                    Location = job.Location,
                    IsRemote = job.IsRemote,
                    SalaryInfo = job.SalaryInfo
                }, ct);

                addedCount++;
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
    }
}
