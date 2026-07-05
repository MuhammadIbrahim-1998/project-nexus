using Microsoft.EntityFrameworkCore;
using Nexus.Domain.Entities;

namespace Nexus.Application.Common.Interfaces;

public interface INexusDbContext
{
    DbSet<Job> Jobs { get; }
    DbSet<Nexus.Domain.Entities.Application> Applications { get; }
    DbSet<AgentLog> AgentLogs { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}