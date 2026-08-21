using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Nexus.Infrastructure.ExternalServices.DeepSeek;

public class DeepSeekMatchingResponse
{
    public int Score { get; set; }
    public string? Reasoning { get; set; }
}

public class DeepSeekMatchingClient
{
    private const string Model = "deepseek-v4-flash";

    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly ILogger<DeepSeekMatchingClient> _logger;

    public DeepSeekMatchingClient(
        HttpClient http,
        IConfiguration config,
        ILogger<DeepSeekMatchingClient> logger)
    {
        _http = http;
        _apiKey = config["DeepSeek:ApiKey"] ?? string.Empty;
        _logger = logger;
    }

    public async Task<DeepSeekMatchingResponse?> MatchJobAsync(
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
                        "You are a job matching assistant. Always respond with ONLY a valid JSON object and no other text, markdown, or code fences."
                },
                new { role = "user", content = prompt }
            },
            temperature = 0.1,
            response_format = new { type = "json_object" }
        });

        using var request = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        using var response = await _http.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        _logger.LogDebug("DeepSeek matching response: {Body}", body);

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

        return ParseContent(messageContent);
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

        return $@"Rate how well this job matches the user's profile. Return ONLY a JSON object exactly like this, with no extra text:
{{""score"": 0-100, ""reasoning"": ""one sentence""}}

Job Title: {jobTitle}
Job Description: {description}

User Profile:
- Skills: {safeSkills}
- Experience: {safeExperience}
- Preferred Roles: {safeRoles}";
    }

    private static DeepSeekMatchingResponse? ParseContent(string content)
    {
        var cleaned = content.Trim();

        // Strip markdown code fences just in case the model wraps the JSON.
        if (cleaned.StartsWith("```"))
        {
            var start = cleaned.IndexOf('{');
            var end = cleaned.LastIndexOf('}');
            if (start >= 0 && end > start)
                cleaned = cleaned[start..(end + 1)];
        }

        using var doc = JsonDocument.Parse(cleaned);
        var root = doc.RootElement;

        if (!root.TryGetProperty("score", out var scoreProp))
            return null;

        var score = 0;
        if (scoreProp.ValueKind == JsonValueKind.Number)
        {
            score = scoreProp.TryGetInt32(out var intScore)
                ? intScore
                : (int)Math.Round(scoreProp.GetDouble());
        }
        else if (scoreProp.ValueKind == JsonValueKind.String && int.TryParse(scoreProp.GetString(), out var parsedScore))
        {
            score = parsedScore;
        }

        var reasoning = root.TryGetProperty("reasoning", out var reasoningProp) &&
                        reasoningProp.ValueKind == JsonValueKind.String
            ? reasoningProp.GetString()
            : null;

        return new DeepSeekMatchingResponse
        {
            Score = Math.Clamp(score, 0, 100),
            Reasoning = reasoning
        };
    }
}