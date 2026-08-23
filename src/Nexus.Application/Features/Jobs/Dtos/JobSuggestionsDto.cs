namespace Nexus.Application.Features.Jobs.Dtos;

public class JobSuggestionsDto
{
    public int JobId { get; set; }
    public string Title { get; set; } = default!;
    public string Company { get; set; } = default!;
    public List<string> MissingSkills { get; set; } = new();
    public List<ProjectSuggestionDto> Suggestions { get; set; } = new();
}

public class ProjectSuggestionDto
{
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public List<string> SkillsAddressed { get; set; } = new();
}
