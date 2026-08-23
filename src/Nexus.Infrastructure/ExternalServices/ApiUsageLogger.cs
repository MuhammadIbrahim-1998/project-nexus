using Microsoft.Extensions.DependencyInjection;
using Nexus.Application.Common.Interfaces;
using Nexus.Domain.Entities;

namespace Nexus.Infrastructure.ExternalServices;

public static class ApiUsageLogger
{
    public static async Task LogAsync(
        IServiceScopeFactory scopeFactory,
        string provider,
        string model,
        int inputTokens,
        int outputTokens,
        int responseTimeMs,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<INexusDbContext>();

            db.ApiUsageLogs.Add(new ApiUsageLog
            {
                Provider = provider,
                Model = model,
                InputTokens = inputTokens,
                OutputTokens = outputTokens,
                TotalTokens = inputTokens + outputTokens,
                ResponseTimeMs = responseTimeMs,
                EstimatedCostUsd = EstimateCost(provider, inputTokens, outputTokens)
            });

            await db.SaveChangesAsync(cancellationToken);
        }
        catch
        {
        }
    }

    private static decimal? EstimateCost(string provider, int inputTokens, int outputTokens)
    {
        return provider switch
        {
            "DeepSeekContent" or "DeepSeekMatching" =>
                Math.Round(
                    inputTokens / 1_000_000m * 0.27m + outputTokens / 1_000_000m * 1.10m,
                    6),
            "Claude" =>
                Math.Round(
                    inputTokens / 1_000_000m * 3.00m + outputTokens / 1_000_000m * 15.00m,
                    6),
            _ => null
        };
    }
}