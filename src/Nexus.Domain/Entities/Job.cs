using Nexus.Domain.Common;

namespace Nexus.Domain.Entities;

public class Job : BaseEntity
{
    public string Title { get; set; } = default!;
    public string Company { get; set; } = default!;
    public string? Description { get; set; }
    public string Source { get; set; } = default!;
    public string? Url { get; set; }
    public string? Location { get; set; }
    public bool IsRemote { get; set; } = true;
    public string? SalaryInfo { get; set; }
    public int? MatchedScore { get; set; }
    public string? MatchReasoning { get; set; }
    public string? GeneratedContent { get; set; }
    public DateTime? ContentGeneratedAt { get; set; }
    public DateTime? PostedDate { get; set; }
    public DateTime DiscoveredAt { get; set; } = DateTime.UtcNow;

    public ICollection<Application> Applications { get; set; } = new List<Application>();
}