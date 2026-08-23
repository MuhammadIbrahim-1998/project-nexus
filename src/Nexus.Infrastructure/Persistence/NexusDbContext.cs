using Microsoft.EntityFrameworkCore;
using Nexus.Application.Common.Interfaces;
using Nexus.Domain.Entities;

namespace Nexus.Infrastructure.Persistence;

public class NexusDbContext : DbContext, INexusDbContext
{
    public NexusDbContext(DbContextOptions<NexusDbContext> options) : base(options) { }

    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<Nexus.Domain.Entities.Application> Applications => Set<Nexus.Domain.Entities.Application>();
    public DbSet<AgentLog> AgentLogs => Set<AgentLog>();
    public DbSet<ApiUsageLog> ApiUsageLogs => Set<ApiUsageLog>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Job>(e =>
        {
            e.Property(j => j.Title).IsRequired().HasMaxLength(300);
            e.Property(j => j.Company).IsRequired().HasMaxLength(200);
            e.Property(j => j.Source).IsRequired().HasMaxLength(100);
            e.Property(j => j.SourceUrl).HasMaxLength(1000);
            e.Property(j => j.Url).HasMaxLength(1000);
            e.Property(j => j.Location).HasMaxLength(200);
            e.Property(j => j.SalaryInfo).HasMaxLength(200);
            e.Property(j => j.MatchReasoning).HasMaxLength(500);
            e.HasIndex(j => new { j.Title, j.Company });
        });

        b.Entity<Nexus.Domain.Entities.Application>(e =>
        {
            e.Property(a => a.Status).HasConversion<string>().HasMaxLength(50);
            e.Property(a => a.CvVersionUsed).HasMaxLength(100);
            e.Property(a => a.CoverLetterVersion).HasMaxLength(100);

            e.HasOne(a => a.Job)
             .WithMany(j => j.Applications)
             .HasForeignKey(a => a.JobId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<AgentLog>(e =>
        {
            e.Property(l => l.AgentType).HasConversion<string>().HasMaxLength(50);
            e.Property(l => l.Status).HasConversion<string>().HasMaxLength(50);
            e.Property(l => l.Result).HasMaxLength(2000);

            e.HasOne(l => l.Job)
             .WithMany()
             .HasForeignKey(l => l.JobId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        b.Entity<ApiUsageLog>(e =>
        {
            e.Property(a => a.Provider).IsRequired().HasMaxLength(50);
            e.Property(a => a.Model).IsRequired().HasMaxLength(100);
            e.Property(a => a.EstimatedCostUsd).HasPrecision(10, 6);
            e.Property(a => a.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

            e.HasOne(a => a.AgentLog)
             .WithMany()
             .HasForeignKey(a => a.AgentLogId)
             .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
