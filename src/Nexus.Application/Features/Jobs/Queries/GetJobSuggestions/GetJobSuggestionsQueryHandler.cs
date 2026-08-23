using MediatR;
using Microsoft.EntityFrameworkCore;
using Nexus.Application.Common.Interfaces;
using Nexus.Application.Features.Jobs.Dtos;

namespace Nexus.Application.Features.Jobs.Queries.GetJobSuggestions;

public class GetJobSuggestionsQueryHandler : IRequestHandler<GetJobSuggestionsQuery, JobSuggestionsDto?>
{
    private readonly INexusDbContext _db;
    private readonly IProjectSuggestionService _suggestionService;

    public GetJobSuggestionsQueryHandler(INexusDbContext db, IProjectSuggestionService suggestionService)
    {
        _db = db;
        _suggestionService = suggestionService;
    }

    public async Task<JobSuggestionsDto?> Handle(GetJobSuggestionsQuery request, CancellationToken cancellationToken)
    {
        var job = await _db.Jobs.FirstOrDefaultAsync(j => j.Id == request.JobId, cancellationToken);
        if (job == null)
        {
            return null;
        }

        var missingSkills = ParseMissingSkills(job.MissingSkills);
        var suggestions = await _suggestionService.SuggestProjectsAsync(missingSkills, cancellationToken);

        return new JobSuggestionsDto
        {
            JobId = job.Id,
            Title = job.Title,
            Company = job.Company,
            MissingSkills = missingSkills,
            Suggestions = suggestions
        };
    }

    private static List<string> ParseMissingSkills(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return new List<string>();
        }

        return raw
            .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
    }
}
