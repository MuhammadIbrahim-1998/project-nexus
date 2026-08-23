using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Nexus.Application.Common.Interfaces;
using Nexus.Application.Common.Models;

namespace Nexus.Infrastructure.Agents.Discovery;

public class ArbeitnowJobDiscoverySource : IJobDiscoverySource
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ArbeitnowJobDiscoverySource> _logger;

    public ArbeitnowJobDiscoverySource(HttpClient httpClient, ILogger<ArbeitnowJobDiscoverySource> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IReadOnlyList<DiscoveredJob>> DiscoverAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("https://www.arbeitnow.com/api/job-board-api?remote=true", cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var payload = JsonSerializer.Deserialize<ArbeitnowResponse>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var items = payload?.Data ?? new List<ArbeitnowJob>();

            var jobs = items
                .Where(j => !string.IsNullOrWhiteSpace(j.Title) && !string.IsNullOrWhiteSpace(j.CompanyName))
                .Select(j => new DiscoveredJob(
                    Title: j.Title!.Trim(),
                    Company: j.CompanyName!.Trim(),
                    Source: "Arbeitnow",
                    Location: string.IsNullOrWhiteSpace(j.Location) ? "Remote" : j.Location!,
                    IsRemote: j.Remote,
                    SalaryInfo: j.Salary,
                    Description: StripHtml(j.Description),
                    Url: j.Url,
                    SourceUrl: j.Url))
                .ToList();

            _logger.LogInformation("Arbeitnow: {Count} job(s) fetched.", jobs.Count);
            return jobs;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Arbeitnow discovery source failed (skipping this source).");
            return Array.Empty<DiscoveredJob>();
        }
    }

    private static string? StripHtml(string? html)
    {
        if (string.IsNullOrWhiteSpace(html)) return null;
        var doc = System.Text.RegularExpressions.Regex.Replace(html, "<[^>]+>", " ");
        return System.Net.WebUtility.HtmlDecode(doc).Trim();
    }

    private class ArbeitnowResponse
    {
        [JsonPropertyName("data")]
        public List<ArbeitnowJob>? Data { get; set; }
    }

    private class ArbeitnowJob
    {
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("company_name")]
        public string? CompanyName { get; set; }

        [JsonPropertyName("location")]
        public string? Location { get; set; }

        [JsonPropertyName("remote")]
        public bool Remote { get; set; }

        [JsonPropertyName("salary")]
        public string? Salary { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }
    }
}