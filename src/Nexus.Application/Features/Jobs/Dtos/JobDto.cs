namespace Nexus.Application.Features.Jobs.Dtos;

public class JobDto
{
    public int Id { get; set; }
    public string Title { get; set; } = default!;
    public string Company { get; set; } = default!;
    public string? Location { get; set; }
    public string Source { get; set; } = default!;
    public bool IsRemote { get; set; }
    public decimal? MatchedScore { get; set; }
    public DateTime DiscoveredAt { get; set; }
}