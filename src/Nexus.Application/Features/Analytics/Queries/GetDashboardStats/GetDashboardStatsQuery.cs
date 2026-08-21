using MediatR;
using Nexus.Application.Features.Analytics.Dtos;

namespace Nexus.Application.Features.Analytics.Queries.GetDashboardStats;

public record GetDashboardStatsQuery : IRequest<DashboardStatsDto>;