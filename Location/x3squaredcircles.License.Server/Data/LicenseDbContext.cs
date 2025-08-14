using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;
using x3squaredcircles.License.Server.Models;

namespace x3squaredcircles.License.Server.Data
{
    public class LicenseDbContext : DbContext
    {
        public LicenseDbContext(DbContextOptions<LicenseDbContext> options) : base(options)
        {
        }

        public DbSet<LicenseConfig> LicenseConfigs { get; set; }
        public DbSet<MonthlyUsage> MonthlyUsages { get; set; }
        public DbSet<ActiveSession> ActiveSessions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // LicenseConfig table configuration
            modelBuilder.Entity<LicenseConfig>(entity =>
            {
                entity.ToTable("license_config");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.MaxConcurrent).IsRequired();
                entity.Property(e => e.ToolsLicensed).IsRequired();
                entity.Property(e => e.BurstMultiplier).HasDefaultValue(2);
                entity.Property(e => e.BurstAllowancePerMonth).HasDefaultValue(2);
            });

            // MonthlyUsage table configuration
            modelBuilder.Entity<MonthlyUsage>(entity =>
            {
                entity.ToTable("monthly_usage");
                entity.HasKey(e => e.Month);
                entity.Property(e => e.Month).HasMaxLength(7); // "2025-01"
                entity.Property(e => e.BurstEventsUsed).HasDefaultValue(0);
            });

            // ActiveSession table configuration
            modelBuilder.Entity<ActiveSession>(entity =>
            {
                entity.ToTable("active_sessions");
                entity.HasKey(e => e.SessionId);
                entity.Property(e => e.SessionId).HasMaxLength(50);
                entity.Property(e => e.ToolName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.ToolVersion).IsRequired().HasMaxLength(20);
                entity.Property(e => e.StartTime).IsRequired();
                entity.Property(e => e.LastHeartbeat).IsRequired();
                entity.Property(e => e.IpAddress).HasMaxLength(45); // IPv6 support
                entity.Property(e => e.BuildId).HasMaxLength(50);

                // Index for performance
                entity.HasIndex(e => e.LastHeartbeat);
                entity.HasIndex(e => e.ToolName);
            });
        }
    }
}