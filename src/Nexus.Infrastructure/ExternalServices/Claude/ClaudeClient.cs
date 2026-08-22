using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nexus.Application.Common.Interfaces;
using Nexus.Infrastructure.ExternalServices;

namespace Nexus.Infrastructure.ExternalServices.Claude;

public class ClaudeClient : IClaudeClient
{
    private readonly HttpClient _http;
    private readonly string _model;
    private readonly IServiceScopeFactory _scopeFactory;

    public ClaudeClient(HttpClient http, IConfiguration config, IServiceScopeFactory scopeFactory)
    {
        _http = http;
        _model = config["Anthropic:Model"] ?? "claude-haiku-4-5-20251001";
        _scopeFactory = scopeFactory;
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
        var stopwatch = Stopwatch.StartNew();
        using var response = await _http.PostAsync("v1/messages", content, cancellationToken);
        response.EnsureSuccessStatusCode();
        stopwatch.Stop();

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(body);

        var sb = new StringBuilder();
        foreach (var block in doc.RootElement.GetProperty("content").EnumerateArray())
        {
            if (block.TryGetProperty("type", out var t) && t.GetString() == "text")
                sb.Append(block.GetProperty("text").GetString());
        }

        var inputTokens = 0;
        var outputTokens = 0;
        if (doc.RootElement.TryGetProperty("usage", out var usage))
        {
            inputTokens = usage.TryGetProperty("input_tokens", out var it) ? it.GetInt32() : 0;
            outputTokens = usage.TryGetProperty("output_tokens", out var ot) ? ot.GetInt32() : 0;
        }

        await ApiUsageLogger.LogAsync(
            _scopeFactory,
            "Claude",
            _model,
            inputTokens,
            outputTokens,
            (int)stopwatch.ElapsedMilliseconds,
            cancellationToken);

        return sb.ToString();
    }
}