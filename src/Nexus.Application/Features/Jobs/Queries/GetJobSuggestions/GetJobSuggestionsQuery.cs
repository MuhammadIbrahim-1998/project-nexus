using MediatR;
using Nexus.Application.Features.Jobs.Dtos;

namespace Nexus.Application.Features.Jobs.Queries.GetJobSuggestions;

public record GetJobSuggestionsQuery(int JobId) : IRequest<JobSuggestionsDto?>;
