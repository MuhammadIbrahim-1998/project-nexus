using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nexus.Application.Common.Interfaces;
using Nexus.Application.Features.Analytics.Queries.GetDashboardStats;
using Nexus.Infrastructure.ExternalServices.DeepSeek;

namespace Nexus.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnalyticsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly INexusDbContext _db;
    private readonly DeepSeekMatchingClient _deepSeek;

    public AnalyticsController(IMediator mediator, INexusDbContext db, DeepSeekMatchingClient deepSeek)
    {
        _mediator = mediator;
        _db = db;
        _deepSeek = deepSeek;
    }

    [HttpGet("dashboard-stats")]
    public async Task<IActionResult> GetDashboardStats()
    {
        var stats = await _mediator.Send(new GetDashboardStatsQuery());
        return Ok(stats);
    }

    [HttpGet("skill-gap-suggestions")]
    public async Task<IActionResult> GetSkillGapSuggestions([FromQuery] int top = 10, CancellationToken cancellationToken = default)
    {
        var topJobs = await _db.Jobs
            .Where(j => j.MatchedScore != null && j.MissingSkills != null)
            .OrderByDescending(j => j.MatchedScore)
            .Take(top)
            .Select(j => j.MissingSkills!)
            .ToListAsync(cancellationToken);

        var missingSkills = topJobs
            .SelectMany(s => s.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(30)
            .ToList();

        if (missingSkills.Count == 0)
        {
            return Ok(new
            {
                missingSkills = Array.Empty<string>(),
                suggestions = new List<ProjectSuggestion>()
            });
        }

        var response = await _deepSeek.SuggestProjectsAsync(missingSkills, cancellationToken);

        return Ok(new
        {
            missingSkills,
            suggestions = response?.Suggestions ?? new List<ProjectSuggestion>()
        });
    }
}