using Nexus.Application.Common.Interfaces;
using Nexus.Application.Common.Models;

namespace Nexus.Infrastructure.Agents.Discovery;

public class DummyJobDiscoverySource : IJobDiscoverySource
{
    public Task<IReadOnlyList<DiscoveredJob>> DiscoverAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<DiscoveredJob> jobs = new List<DiscoveredJob>
        {
            new("Remote .NET Backend Engineer", "Nimbus Labs", "DummySource",
                "Remote (US)", true, "$90k-$120k",
                "Build APIs with .NET and Azure.", "https://example.com/jobs/1"),
            new("Senior C# Developer", "Orbit Systems", "DummySource",
                "Remote (Worldwide)", true, "$100k-$130k",
                "Clean Architecture, CQRS, EF Core.", "https://example.com/jobs/2"),
            new(".NET Full Stack Developer", "Vertex Digital", "DummySource",
                "Remote (EU)", true, "70k-90k EUR",
                "React + .NET, SignalR realtime.", "https://example.com/jobs/3"),
        };

        return Task.FromResult(jobs);
    }
}
