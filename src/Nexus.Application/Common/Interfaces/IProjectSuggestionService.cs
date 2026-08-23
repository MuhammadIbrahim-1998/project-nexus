using Nexus.Application.Features.Jobs.Dtos;

namespace Nexus.Application.Common.Interfaces;

public interface IProjectSuggestionService
{
    Task<List<ProjectSuggestionDto>> SuggestProjectsAsync(
        IReadOnlyList<string> missingSkills,
        CancellationToken cancellationToken = default);
}
