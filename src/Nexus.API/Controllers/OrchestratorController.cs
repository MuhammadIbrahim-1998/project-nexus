using Microsoft.AspNetCore.Mvc;
using Nexus.Infrastructure.Agents.Orchestrator;

namespace Nexus.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrchestratorController : ControllerBase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OrchestratorController> _logger;

    public OrchestratorController(IServiceScopeFactory scopeFactory, ILogger<OrchestratorController> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    [HttpPost("run-full-cycle")]
    public IActionResult RunFullCycle()
    {
        var runId = Guid.NewGuid();
        _ = Task.Run(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var orchestrator = scope.ServiceProvider.GetRequiredService<NexusOrchestratorService>();
            try
            {
                var summary = await orchestrator.RunFullPipelineAsync(CancellationToken.None);
                _logger.LogInformation("Orchestrator pipeline {RunId} finished with status {Status}", summary.RunId, summary.Status);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Orchestrator pipeline {RunId} was cancelled.", runId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Orchestrator pipeline {RunId} failed.", runId);
            }
        }, CancellationToken.None);

        return Accepted(new
        {
            RunId = runId,
            Status = "Accepted",
            Message = "Pipeline started in the background. Progress is broadcast over the AgentStatusHub."
        });
    }
}