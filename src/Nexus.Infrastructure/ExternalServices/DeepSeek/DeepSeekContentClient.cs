using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Nexus.Infrastructure.ExternalServices.DeepSeek;

public class DeepSeekContentClient
{
    private const string Model = "deepseek-v4-flash";

    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly ILogger<DeepSeekContentClient> _logger;

    public DeepSeekContentClient(
        HttpClient http,
        IConfiguration config,
        ILogger<DeepSeekContentClient> logger)
    {
        _http = http;
        _apiKey = config["DeepSeek:ApiKey"] ?? string.Empty;
        _logger = logger;
    }

    public async Task<string?> GenerateContentAsync(
        string jobTitle,
        string? jobDescription,
        string skills,
        string experience,
        string preferredRoles,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            throw new InvalidOperationException("DeepSeek:ApiKey is not configured. Set it via user-secrets.");
        }

        var prompt = BuildPrompt(jobTitle, jobDescription, skills, experience, preferredRoles);

        var payload = JsonSerializer.Serialize(new
        {
            model = Model,
            messages = new[]
            {
                new
                {
                    role = "system",
                    content =
                        "You are a resume and cover letter writing assistant. Respond with plain, readable formatted text only - no JSON, no markdown code fences."
                },
                new { role = "user", content = prompt }
            },
            temperature = 0.4
        });

        using var request = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        using var response = await _http.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        _logger.LogDebug("DeepSeek content generation response: {Body}", body);

        using var doc = JsonDocument.Parse(body);
        if (!doc.RootElement.TryGetProperty("choices", out var choices) || choices.GetArrayLength() == 0)
        {
            _logger.LogWarning("DeepSeek response contained no choices.");
            return null;
        }

        var messageContent = choices[0].GetProperty("message").GetProperty("content").GetString();
        if (string.IsNullOrWhiteSpace(messageContent))
        {
            _logger.LogWarning("DeepSeek response content was empty.");
            return null;
        }

        return messageContent.Trim();
    }

    private static string BuildPrompt(
        string jobTitle,
        string? jobDescription,
        string skills,
        string experience,
        string preferredRoles)
    {
        var description = string.IsNullOrWhiteSpace(jobDescription) ? "Not provided" : jobDescription;
        var safeSkills = string.IsNullOrWhiteSpace(skills) ? "Not provided" : skills;
        var safeExperience = string.IsNullOrWhiteSpace(experience) ? "Not provided" : experience;
        var safeRoles = string.IsNullOrWhiteSpace(preferredRoles) ? "Not provided" : preferredRoles;

        return $@"Based on the job description below and the user's profile, generate application content for the job '{jobTitle}'.

Requirements:
1. Write 3-4 tailored CV bullet points that highlight the most relevant skills from the user's profile for THIS specific job.
2. Write a short 2-3 sentence cover letter opening paragraph tailored to this job's description.
3. Return everything as plain, readable formatted text (no JSON, no markdown code fences). Use simple headings to separate the two sections.

Job Title: {jobTitle}
Job Description: {description}

User Profile:
- Skills: {safeSkills}
- Experience: {safeExperience}
- Preferred Roles: {safeRoles}";
    }
}