using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Nexus.Infrastructure.Persistence;

public class NexusDbContextFactory : IDesignTimeDbContextFactory<NexusDbContext>
{
    public NexusDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<NexusDbContext>()
            .UseSqlServer("Server=localhost\\SQLEXPRESS;Database=ProjectNexus;Trusted_Connection=True;TrustServerCertificate=True;")
            .Options;

        return new NexusDbContext(options);
    }
}