using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Nexus.Application.Common.Interfaces;
using Nexus.Application.Common.Models;

namespace Nexus.Infrastructure.Agents.Discovery;

public class RemoteOkJobDiscoverySource : IJobDiscoverySource
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<RemoteOkJobDiscoverySource> _logger;

    public RemoteOkJobDiscoverySource(HttpClient httpClient, ILogger<RemoteOkJobDiscoverySource> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IReadOnlyList<DiscoveredJob>> DiscoverAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("https://remoteok.com/api", cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var items = JsonSerializer.Deserialize<List<RemoteOkJob>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();

            var jobs = items
                .Where(j => !string.IsNullOrWhiteSpace(j.Position) && !string.IsNullOrWhiteSpace(j.Company))
                .Select(j => new DiscoveredJob(
                    Title: j.Position!.Trim(),
                    Company: j.Company!.Trim(),
                    Source: "RemoteOK",
                    Location: string.IsNullOrWhiteSpace(j.Location) ? "Remote" : j.Location,
                    IsRemote: true,
                    SalaryInfo: null,
                    Description: StripHtml(j.Description),
                    Url: string.IsNullOrWhiteSpace(j.Url) ? j.ApplyUrl : j.Url,
                    SourceUrl: string.IsNullOrWhiteSpace(j.Url) ? j.ApplyUrl : j.Url))
                .ToList();

            _logger.LogInformation("RemoteOK: {Count} job(s) fetched.", jobs.Count);
            return jobs;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "RemoteOK discovery source failed (skipping this source).");
            return Array.Empty<DiscoveredJob>();
        }
    }

    private static string? StripHtml(string? html)
    {
        if (string.IsNullOrWhiteSpace(html)) return null;
        var doc = System.Text.RegularExpressions.Regex.Replace(html, "<[^>]+>", " ");
        return System.Net.WebUtility.HtmlDecode(doc).Trim();
    }

    private class RemoteOkJob
    {
        [JsonPropertyName("position")]
        public string? Position { get; set; }

        [JsonPropertyName("company")]
        public string? Company { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("location")]
        public string? Location { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("apply_url")]
        public string? ApplyUrl { get; set; }
    }
}