using MediatR;
using Microsoft.EntityFrameworkCore;
using Nexus.Application.Common.Interfaces;
using Nexus.Application.Features.Analytics.Dtos;

namespace Nexus.Application.Features.Analytics.Queries.GetDashboardStats;

public class GetDashboardStatsQueryHandler : IRequestHandler<GetDashboardStatsQuery, DashboardStatsDto>
{
    private readonly INexusDbContext _db;

    public GetDashboardStatsQueryHandler(INexusDbContext db) => _db = db;

    public async Task<DashboardStatsDto> Handle(GetDashboardStatsQuery request, CancellationToken cancellationToken)
    {
        var jobRows = await _db.Jobs
            .Select(j => new { j.MatchedScore, j.GeneratedContent })
            .ToListAsync(cancellationToken);

        var scoredScores = jobRows
            .Where(j => j.MatchedScore.HasValue)
            .Select(j => j.MatchedScore!.Value)
            .ToList();

        var recentAgentRuns = await _db.AgentLogs
            .OrderByDescending(a => a.RunAt)
            .Take(10)
            .Select(a => new AgentRunDto
            {
                AgentType = a.AgentType,
                Status = a.Status,
                RunAt = a.RunAt,
                DurationMs = a.DurationMs,
                Result = a.Result
            })
            .ToListAsync(cancellationToken);

        return new DashboardStatsDto
        {
            TotalJobsDiscovered = jobRows.Count,
            AverageMatchScore = scoredScores.Count > 0 ? scoredScores.Average() : null,
            HighMatchCount = scoredScores.Count(s => s >= 80),
            MediumMatchCount = scoredScores.Count(s => s >= 50 && s < 80),
            LowMatchCount = scoredScores.Count(s => s < 50),
            TotalContentGenerated = jobRows.Count(j => j.GeneratedContent != null),
            RecentAgentRuns = recentAgentRuns
        };
    }
}