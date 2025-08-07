using Microsoft.EntityFrameworkCore;
using ScheduleSystem.Models;

namespace ScheduleSystem.Data;

/// <summary>
/// Entity Framework database context for the scheduling system
/// </summary>
public class ScheduleDbContext : DbContext
{
    public ScheduleDbContext(DbContextOptions<ScheduleDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Meeting> Meetings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure User entity
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Name).IsRequired().HasMaxLength(100);
        });

        // Configure Meeting entity
        modelBuilder.Entity<Meeting>(entity =>
        {
            entity.HasKey(m => m.Id);
            entity.Property(m => m.StartTime).IsRequired();
            entity.Property(m => m.EndTime).IsRequired();
            
            // Configure many-to-many relationship
            entity.HasMany(m => m.Participants)
                  .WithMany(u => u.Meetings);
        });
    }
}