using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Nexus.Application.Common.Interfaces;
using Nexus.Application.Common.Models;

namespace Nexus.Infrastructure.Agents.Discovery;

public class HimalayasJobDiscoverySource : IJobDiscoverySource
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HimalayasJobDiscoverySource> _logger;

    public HimalayasJobDiscoverySource(HttpClient httpClient, ILogger<HimalayasJobDiscoverySource> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IReadOnlyList<DiscoveredJob>> DiscoverAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("https://himalayas.app/jobs/api?limit=100", cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var payload = JsonSerializer.Deserialize<HimalayasResponse>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var items = payload?.Jobs ?? new List<HimalayasJob>();

            var jobs = items
                .Where(j => !string.IsNullOrWhiteSpace(j.Title) && !string.IsNullOrWhiteSpace(j.CompanyName))
                .Select(j => new DiscoveredJob(
                    Title: j.Title!.Trim(),
                    Company: j.CompanyName!.Trim(),
                    Source: "Himalayas",
                    Location: BuildLocation(j.LocationRestrictions),
                    IsRemote: true,
                    SalaryInfo: BuildSalaryInfo(j.MinSalary, j.MaxSalary, j.Currency, j.SalaryPeriod),
                    Description: StripHtml(j.Description),
                    Url: j.ApplicationLink,
                    SourceUrl: j.ApplicationLink ?? j.Guid))
                .ToList();

            _logger.LogInformation("Himalayas: {Count} job(s) fetched.", jobs.Count);
            return jobs;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Himalayas discovery source failed (skipping this source).");
            return Array.Empty<DiscoveredJob>();
        }
    }

    private static string? BuildLocation(List<string>? restrictions)
    {
        if (restrictions is null || restrictions.Count == 0) return "Remote";
        return $"Remote ({string.Join(", ", restrictions)})";
    }

    private static string? BuildSalaryInfo(decimal? min, decimal? max, string? currency, string? period)
    {
        if (min is null && max is null) return null;
        string range = min is null ? $"up to {max}" :
                       max is null ? $"from {min}" : $"{min}-{max}";
        var parts = new List<string> { range };
        if (!string.IsNullOrWhiteSpace(currency)) parts.Add(currency.Trim());
        if (!string.IsNullOrWhiteSpace(period)) parts.Add(period.Trim());
        return string.Join(" ", parts);
    }

    private static string? StripHtml(string? html)
    {
        if (string.IsNullOrWhiteSpace(html)) return null;
        var doc = System.Text.RegularExpressions.Regex.Replace(html, "<[^>]+>", " ");
        return System.Net.WebUtility.HtmlDecode(doc).Trim();
    }

    private class HimalayasResponse
    {
        [JsonPropertyName("jobs")]
        public List<HimalayasJob>? Jobs { get; set; }
    }

    private class HimalayasJob
    {
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("companyName")]
        public string? CompanyName { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("locationRestrictions")]
        public List<string>? LocationRestrictions { get; set; }

        [JsonPropertyName("currency")]
        public string? Currency { get; set; }

        [JsonPropertyName("minSalary")]
        public decimal? MinSalary { get; set; }

        [JsonPropertyName("maxSalary")]
        public decimal? MaxSalary { get; set; }

        [JsonPropertyName("salaryPeriod")]
        public string? SalaryPeriod { get; set; }

        [JsonPropertyName("applicationLink")]
        public string? ApplicationLink { get; set; }

        [JsonPropertyName("guid")]
        public string? Guid { get; set; }
    }
}