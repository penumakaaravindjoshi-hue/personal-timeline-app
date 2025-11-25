using personal_timeline_backend.Models;
using Microsoft.EntityFrameworkCore;

namespace personal_timeline_backend.Data;

public class TimelineContext : DbContext
{
    public TimelineContext(DbContextOptions<TimelineContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<TimelineEntry> TimelineEntries { get; set; }
    public DbSet<ApiConnection> ApiConnections { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).IsRequired();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.OAuthId).IsRequired();
            entity.HasIndex(e => new { e.OAuthProvider, e.OAuthId }).IsUnique();
        });

        modelBuilder.Entity<TimelineEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired();
            entity.Property(e => e.EventDate).IsRequired();
            entity.HasOne(d => d.User)
                .WithMany(p => p.TimelineEntries)
                .HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<ApiConnection>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.ApiProvider }).IsUnique();
            entity.HasOne(d => d.User)
                .WithMany(p => p.ApiConnections)
                .HasForeignKey(d => d.UserId);
        });
    }
}
