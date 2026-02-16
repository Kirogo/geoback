using Microsoft.EntityFrameworkCore;
using geoback.Models;

namespace geoback.Data;

public class GeoDbContext : DbContext
{
    public GeoDbContext(DbContextOptions<GeoDbContext> options) : base(options) { }

    public DbSet<Client> Clients { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<SiteVisitReport> Reports { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Client>()
            .HasIndex(c => c.CustomerNumber)
            .IsUnique();

        modelBuilder.Entity<Client>()
            .HasIndex(c => c.Email)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<SiteVisitReport>()
            .Property(r => r.Status)
            .HasConversion<string>();
        
        // Configuring owned types for lists of simple objects (simplified for this stage)
        // In a real production app we might make these separate tables with FKs
        // For now, we'll store them as JSON if the database supports it, or separate tables.
        // Let's use separate tables for cleaner normalization given it's MySQL.
        
        modelBuilder.Entity<SiteVisitReport>()
            .OwnsMany(r => r.WorkProgress, wp =>
            {
                wp.ToTable("WorkProgress");
                wp.WithOwner().HasForeignKey("ReportId");
                wp.HasKey("Id");
            });

        modelBuilder.Entity<SiteVisitReport>()
            .OwnsMany(r => r.Issues, i =>
            {
                i.ToTable("Issues");
                i.WithOwner().HasForeignKey("ReportId");
                i.HasKey("Id");
            });

        modelBuilder.Entity<SiteVisitReport>()
            .OwnsMany(r => r.Photos, p =>
            {
                p.ToTable("ReportPhotos");
                p.WithOwner().HasForeignKey("ReportId");
                p.HasKey("Id");
            });
    }
}
