using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Nexus.Application.Common.Interfaces;

namespace Nexus.Infrastructure.ExternalServices.Claude;

public class ClaudeClient : IClaudeClient
{
    private readonly HttpClient _http;
    private readonly string _model;

    public ClaudeClient(HttpClient http, IConfiguration config)
    {
        _http = http;
        _model = config["Anthropic:Model"] ?? "claude-haiku-4-5-20251001";
    }

    public async Task<string> CompleteAsync(string prompt, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Serialize(new
        {
            model = _model,
            max_tokens = 1024,
            messages = new[] { new { role = "user", content = prompt } }
        });

        using var content = new StringContent(payload, Encoding.UTF8, "application/json");
        using var response = await _http.PostAsync("v1/messages", content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(body);

        var sb = new StringBuilder();
        foreach (var block in doc.RootElement.GetProperty("content").EnumerateArray())
        {
            if (block.TryGetProperty("type", out var t) && t.GetString() == "text")
                sb.Append(block.GetProperty("text").GetString());
        }
        return sb.ToString();
    }
}
