using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexus.Infrastructure.ExternalServices;

namespace Nexus.Infrastructure.ExternalServices.DeepSeek;

public class DeepSeekMatchingResponse
{
    public int Score { get; set; }
    public string? Reasoning { get; set; }
    public string[]? MissingSkills { get; set; }
}

public class DeepSeekMatchingClient
{
    private const string Model = "deepseek-v4-flash";

    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly ILogger<DeepSeekMatchingClient> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public DeepSeekMatchingClient(
        HttpClient http,
        IConfiguration config,
        ILogger<DeepSeekMatchingClient> logger,
        IServiceScopeFactory scopeFactory)
    {
        _http = http;
        _apiKey = config["DeepSeek:ApiKey"] ?? string.Empty;
        _logger = logger;
        _scopeFactory = scopeFactory;
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

        var stopwatch = Stopwatch.StartNew();
        using var response = await _http.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        stopwatch.Stop();

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

        var inputTokens = 0;
        var outputTokens = 0;
        if (doc.RootElement.TryGetProperty("usage", out var usage))
        {
            inputTokens = usage.TryGetProperty("prompt_tokens", out var pt) ? pt.GetInt32() : 0;
            outputTokens = usage.TryGetProperty("completion_tokens", out var ct) ? ct.GetInt32() : 0;
        }

        await ApiUsageLogger.LogAsync(
            _scopeFactory,
            "DeepSeekMatching",
            Model,
            inputTokens,
            outputTokens,
            (int)stopwatch.ElapsedMilliseconds,
            cancellationToken);

        return ParseContent(messageContent);
    }

    public async Task<DeepSeekSuggestionResponse?> SuggestProjectsAsync(
        IReadOnlyList<string> missingSkills,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            throw new InvalidOperationException("DeepSeek:ApiKey is not configured. Set it via user-secrets.");
        }

        var prompt = BuildSuggestionPrompt(missingSkills);

        var payload = JsonSerializer.Serialize(new
        {
            model = Model,
            messages = new[]
            {
                new
                {
                    role = "system",
                    content =
                        "You are a career advisor that helps developers close skill gaps with practical projects. Always respond with ONLY a valid JSON object and no other text, markdown, or code fences."
                },
                new { role = "user", content = prompt }
            },
            temperature = 0.7,
            response_format = new { type = "json_object" }
        });

        using var request = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        var stopwatch = Stopwatch.StartNew();
        using var response = await _http.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        stopwatch.Stop();

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        _logger.LogDebug("DeepSeek suggestion response: {Body}", body);

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

        var inputTokens = 0;
        var outputTokens = 0;
        if (doc.RootElement.TryGetProperty("usage", out var usage))
        {
            inputTokens = usage.TryGetProperty("prompt_tokens", out var pt) ? pt.GetInt32() : 0;
            outputTokens = usage.TryGetProperty("completion_tokens", out var ct) ? ct.GetInt32() : 0;
        }

        await ApiUsageLogger.LogAsync(
            _scopeFactory,
            "DeepSeekSuggestions",
            Model,
            inputTokens,
            outputTokens,
            (int)stopwatch.ElapsedMilliseconds,
            cancellationToken);

        return ParseSuggestionContent(messageContent);
    }

    private static string BuildSuggestionPrompt(IReadOnlyList<string> missingSkills)
    {
        var skills = missingSkills.Count > 0 ? string.Join(", ", missingSkills) : "Not provided";
        return $@"The user wants to close these skill gaps: {skills}.
Suggest 1 to 2 concrete, self-contained project ideas that will help the user learn and practice these skills. Return ONLY a JSON object exactly like this, with no extra text:
{{""suggestions"": [{{""title"": ""project title"", ""description"": ""one paragraph"", ""skillsAddressed"": [""skill"", ""skill""]}}]}}";
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
{{""score"": 0-100, ""reasoning"": ""one sentence"", ""missingSkills"": [""skill name"", ""skill name""]}}

missingSkills must list every skill required by the job that the user does not have. Derive the required skills from the job description and compare them against the user's skills below.

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

        var missingSkills = new List<string>();
        if (root.TryGetProperty("missingSkills", out var missingProp) && missingProp.ValueKind == JsonValueKind.Array)
        {
            foreach (var skill in missingProp.EnumerateArray())
            {
                if (skill.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(skill.GetString()))
                    missingSkills.Add(skill.GetString()!);
            }
        }

        return new DeepSeekMatchingResponse
        {
            Score = Math.Clamp(score, 0, 100),
            Reasoning = reasoning,
            MissingSkills = missingSkills.Count > 0 ? missingSkills.ToArray() : null
        };
    }

    private static DeepSeekSuggestionResponse? ParseSuggestionContent(string content)
    {
        var cleaned = content.Trim();

        if (cleaned.StartsWith("```"))
        {
            var start = cleaned.IndexOf('{');
            var end = cleaned.LastIndexOf('}');
            if (start >= 0 && end > start)
                cleaned = cleaned[start..(end + 1)];
        }

        using var doc = JsonDocument.Parse(cleaned);
        var root = doc.RootElement;

        if (!root.TryGetProperty("suggestions", out var suggestionsProp) || suggestionsProp.ValueKind != JsonValueKind.Array)
            return new DeepSeekSuggestionResponse();

        var suggestions = new List<ProjectSuggestion>();
        foreach (var item in suggestionsProp.EnumerateArray())
        {
            var title = item.TryGetProperty("title", out var titleProp) && titleProp.ValueKind == JsonValueKind.String
                ? titleProp.GetString()
                : null;
            var description = item.TryGetProperty("description", out var descProp) && descProp.ValueKind == JsonValueKind.String
                ? descProp.GetString()
                : null;

            var skills = new List<string>();
            if (item.TryGetProperty("skillsAddressed", out var skillsProp) && skillsProp.ValueKind == JsonValueKind.Array)
            {
                foreach (var skill in skillsProp.EnumerateArray())
                {
                    if (skill.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(skill.GetString()))
                        skills.Add(skill.GetString()!);
                }
            }

            if (!string.IsNullOrWhiteSpace(title))
            {
                suggestions.Add(new ProjectSuggestion
                {
                    Title = title,
                    Description = description,
                    SkillsAddressed = skills
                });
            }
        }

        return new DeepSeekSuggestionResponse { Suggestions = suggestions };
    }
}

public class DeepSeekSuggestionResponse
{
    public List<ProjectSuggestion>? Suggestions { get; set; }
}

public class ProjectSuggestion
{
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public List<string> SkillsAddressed { get; set; } = new();
}