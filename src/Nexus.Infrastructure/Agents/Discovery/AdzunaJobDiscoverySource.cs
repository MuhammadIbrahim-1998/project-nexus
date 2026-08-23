using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nexus.Application.Common.Interfaces;
using Nexus.Application.Common.Models;

namespace Nexus.Infrastructure.Agents.Discovery;

public class AdzunaJobDiscoverySource : IJobDiscoverySource
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AdzunaJobDiscoverySource> _logger;
    private readonly string _appId;
    private readonly string _appKey;

    public AdzunaJobDiscoverySource(
        HttpClient httpClient,
        IConfiguration config,
        ILogger<AdzunaJobDiscoverySource> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _appId = config["Adzuna:AppId"] ?? string.Empty;
        _appKey = config["Adzuna:AppKey"] ?? string.Empty;
    }

    public async Task<IReadOnlyList<DiscoveredJob>> DiscoverAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_appId) || string.IsNullOrWhiteSpace(_appKey))
        {
            _logger.LogWarning("Adzuna: AppId or AppKey missing — skipping Adzuna source.");
            return Array.Empty<DiscoveredJob>();
        }

        try
        {
            string query = "backend software engineer";
            string url = $"https://api.adzuna.com/v1/api/jobs/us/search/1" +
                         $"?app_id={Uri.EscapeDataString(_appId)}" +
                         $"&app_key={Uri.EscapeDataString(_appKey)}" +
                         $"&results_per_page=50&content-type=application/json" +
                         $"&what={Uri.EscapeDataString(query)}" +
                         $"&max_days_old=30";

            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            var json = System.Text.Encoding.UTF8.GetString(bytes);
            var payload = JsonSerializer.Deserialize<AdzunaResponse>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var items = payload?.Results ?? new List<AdzunaJob>();

            var jobs = items
                .Where(j => !string.IsNullOrWhiteSpace(j.Title) && !string.IsNullOrWhiteSpace(j.Company?.DisplayName))
                .Select(j => new DiscoveredJob(
                    Title: j.Title!.Trim(),
                    Company: j.Company!.DisplayName!.Trim(),
                    Source: "Adzuna",
                    Location: j.Location?.Area?.FirstOrDefault() ?? "Remote",
                    IsRemote: true,
                    SalaryInfo: BuildSalaryInfo(j.SalaryMin, j.SalaryMax, j.SalaryIsPredicted),
                    Description: j.Description,
                    Url: j.RedirectUrl,
                    SourceUrl: j.RedirectUrl))
                .ToList();

            _logger.LogInformation("Adzuna: {Count} job(s) fetched.", jobs.Count);
            return jobs;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Adzuna discovery source failed (skipping this source).");
            return Array.Empty<DiscoveredJob>();
        }
    }

    private static string? BuildSalaryInfo(decimal? min, decimal? max, bool predicted)
    {
        if (min is null && max is null) return null;
        string range = min is null ? $"up to {max}" :
                       max is null ? $"from {min}" : $"{min}-{max}";
        return predicted ? $"{range} (estimated)" : range;
    }

    private class AdzunaResponse
    {
        [JsonPropertyName("results")]
        public List<AdzunaJob>? Results { get; set; }
    }

    private class AdzunaJob
    {
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("company")]
        public AdzunaCompany? Company { get; set; }

        [JsonPropertyName("location")]
        public AdzunaLocation? Location { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("redirect_url")]
        public string? RedirectUrl { get; set; }

        [JsonPropertyName("salary_min")]
        public decimal? SalaryMin { get; set; }

        [JsonPropertyName("salary_max")]
        public decimal? SalaryMax { get; set; }

        [JsonPropertyName("salary_is_predicted")]
        public JsonElement? SalaryIsPredictedElement { get; set; }

        public bool SalaryIsPredicted
        {
            get
            {
                if (SalaryIsPredictedElement is null) return false;
                if (SalaryIsPredictedElement.Value.ValueKind == JsonValueKind.True) return true;
                if (SalaryIsPredictedElement.Value.ValueKind == JsonValueKind.False) return false;
                var raw = SalaryIsPredictedElement.Value.GetString();
                return bool.TryParse(raw, out var parsed) && parsed;
            }
        }
    }

    private class AdzunaCompany
    {
        [JsonPropertyName("display_name")]
        public string? DisplayName { get; set; }
    }

    private class AdzunaLocation
    {
        [JsonPropertyName("area")]
        public List<string>? Area { get; set; }
    }
}