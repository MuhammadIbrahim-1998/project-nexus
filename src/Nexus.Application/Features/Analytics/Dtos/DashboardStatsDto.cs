namespace Nexus.Application.Features.Analytics.Dtos;

public class DashboardStatsDto
{
    public int TotalJobsDiscovered { get; set; }
    public double? AverageMatchScore { get; set; }
    public int HighMatchCount { get; set; }
    public int MediumMatchCount { get; set; }
    public int LowMatchCount { get; set; }
    public int TotalContentGenerated { get; set; }
    public List<AgentRunDto> RecentAgentRuns { get; set; } = new();
}