using System.Text.Json;
using Nexus.Application.Common.Interfaces;
using Nexus.Application.Common.Models;

namespace Nexus.Infrastructure.Agents.Discovery;

public class ClaudeJobDiscoverySource : IJobDiscoverySource
{
    private readonly IClaudeClient _claude;

    public ClaudeJobDiscoverySource(IClaudeClient claude) => _claude = claude;

    public async Task<IReadOnlyList<DiscoveredJob>> DiscoverAsync(CancellationToken cancellationToken = default)
    {
        var prompt = """
            Generate 3 realistic but FICTIONAL remote .NET developer job listings.
            Return ONLY a JSON array (no markdown fences, no commentary).
            Each object must have exactly these string fields:
            title, company, location, salaryInfo, description, url.
            Keep each description under 200 characters.
            """;

        var raw = await _claude.CompleteAsync(prompt, cancellationToken);
        var json = ExtractArray(raw);

        var items = JsonSerializer.Deserialize<List<ClaudeJob>>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();

        return items.Select(j => new DiscoveredJob(
            Title: j.Title ?? "Untitled",
            Company: j.Company ?? "Unknown",
            Source: "AI-Sample",
            Location: j.Location,
            IsRemote: true,
            SalaryInfo: j.SalaryInfo,
            Description: j.Description,
            Url: j.Url,
            SourceUrl: j.Url)).ToList();
    }

    private static string ExtractArray(string s)
    {
        int first = s.IndexOf('[');
        int last = s.LastIndexOf(']');
        return (first >= 0 && last > first) ? s.Substring(first, last - first + 1) : s;
    }

    private class ClaudeJob
    {
        public string? Title { get; set; }
        public string? Company { get; set; }
        public string? Location { get; set; }
        public string? SalaryInfo { get; set; }
        public string? Description { get; set; }
        public string? Url { get; set; }
    }
}
