using Microsoft.Extensions.Logging;
using Nexus.Application.Common.Interfaces;
using Nexus.Application.Common.Models;

namespace Nexus.Infrastructure.Agents.Discovery;

public class CompositeJobDiscoverySource : IJobDiscoverySource
{
    private readonly RemoteOkJobDiscoverySource _remoteOk;
    private readonly HimalayasJobDiscoverySource _himalayas;
    private readonly ArbeitnowJobDiscoverySource _arbeitnow;
    private readonly ILogger<CompositeJobDiscoverySource> _logger;

    public CompositeJobDiscoverySource(
        RemoteOkJobDiscoverySource remoteOk,
        HimalayasJobDiscoverySource himalayas,
        ArbeitnowJobDiscoverySource arbeitnow,
        ILogger<CompositeJobDiscoverySource> logger)
    {
        _remoteOk = remoteOk;
        _himalayas = himalayas;
        _arbeitnow = arbeitnow;
        _logger = logger;
    }

    public async Task<IReadOnlyList<DiscoveredJob>> DiscoverAsync(CancellationToken cancellationToken = default)
    {
        var allJobs = new List<DiscoveredJob>();
        var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Adzuna temporarily disabled: force-sets IsRemote=true + queries US job board only,
        // which polluted the table with non-global-remote jobs. Re-enable by restoring
        // the field/ctor param, the Program.cs DI registration, and the entry below.
        var sources = new (string Name, IJobDiscoverySource Source)[]
        {
            ("RemoteOK", _remoteOk),
            ("Himalayas", _himalayas),
            ("Arbeitnow", _arbeitnow),
        };

        foreach (var (name, source) in sources)
        {
            IReadOnlyList<DiscoveredJob> fetched;
            try
            {
                fetched = await source.DiscoverAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "{Source} discovery failed — skipping this source.", name);
                continue;
            }

            var relevant = fetched.Where(JobRelevanceFilter.IsRelevant).ToList();
            var newCount = 0;

            foreach (var job in relevant)
            {
                var key = $"{job.Title}|{job.Company}";
                if (seenKeys.Add(key))
                {
                    newCount++;
                    allJobs.Add(job);
                }
            }

            _logger.LogInformation(
                "{Source}: {Total} fetched, {Relevant} relevant, {New} new after dedup.",
                name,
                fetched.Count,
                relevant.Count,
                newCount);
        }

        _logger.LogInformation(
            "Composite discovery complete: {Total} relevant job(s) after filtering and dedup.",
            allJobs.Count);

        return allJobs;
    }
}
