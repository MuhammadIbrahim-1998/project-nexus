using Nexus.Application.Common.Models;

namespace Nexus.Application.Common.Interfaces;

public interface IJobDiscoverySource
{
    Task<IReadOnlyList<DiscoveredJob>> DiscoverAsync(CancellationToken cancellationToken = default);
}
