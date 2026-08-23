using Nexus.Application.Common.Interfaces;
using Nexus.Application.Features.Jobs.Dtos;

namespace Nexus.Infrastructure.ExternalServices.DeepSeek;

public class ProjectSuggestionService : IProjectSuggestionService
{
    private readonly DeepSeekMatchingClient _client;

    public ProjectSuggestionService(DeepSeekMatchingClient client) => _client = client;

    public async Task<List<ProjectSuggestionDto>> SuggestProjectsAsync(
        IReadOnlyList<string> missingSkills,
        CancellationToken cancellationToken = default)
    {
        var response = await _client.SuggestProjectsAsync(missingSkills, cancellationToken);
        if (response?.Suggestions == null)
        {
            return new List<ProjectSuggestionDto>();
        }

        return response.Suggestions
            .Select(s => new ProjectSuggestionDto
            {
                Title = s.Title,
                Description = s.Description,
                SkillsAddressed = s.SkillsAddressed
            })
            .ToList();
    }
}
