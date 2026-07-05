using MediatR;
using Microsoft.EntityFrameworkCore;
using Nexus.Application.Common.Interfaces;
using Nexus.Application.Features.Jobs.Dtos;

namespace Nexus.Application.Features.Jobs.Queries.GetAllJobs;

public class GetAllJobsQueryHandler : IRequestHandler<GetAllJobsQuery, List<JobDto>>
{
    private readonly INexusDbContext _db;

    public GetAllJobsQueryHandler(INexusDbContext db) => _db = db;

    public async Task<List<JobDto>> Handle(GetAllJobsQuery request, CancellationToken cancellationToken)
    {
        return await _db.Jobs
            .OrderByDescending(j => j.DiscoveredAt)
            .Select(j => new JobDto
            {
                Id = j.Id,
                Title = j.Title,
                Company = j.Company,
                Location = j.Location,
                Source = j.Source,
                IsRemote = j.IsRemote,
                MatchedScore = j.MatchedScore,
                DiscoveredAt = j.DiscoveredAt
            })
            .ToListAsync(cancellationToken);
    }
}