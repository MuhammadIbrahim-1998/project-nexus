using MediatR;
using Nexus.Application.Features.Jobs.Dtos;

namespace Nexus.Application.Features.Jobs.Queries.GetAllJobs;

public record GetAllJobsQuery : IRequest<List<JobDto>>;
