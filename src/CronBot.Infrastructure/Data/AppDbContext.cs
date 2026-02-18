using System.Reflection;
using CronBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using TaskEntity = CronBot.Domain.Entities.Task;

namespace CronBot.Infrastructure.Data;

/// <summary>
/// Main database context for CronBot.
/// </summary>
public class AppDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AppDbContext"/> class.
    /// </summary>
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // Users & Teams
    public DbSet<User> Users => Set<User>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();

    // Projects
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();

    // Kanban
    public DbSet<Board> Boards => Set<Board>();
    public DbSet<Sprint> Sprints => Set<Sprint>();
    public DbSet<TaskEntity> Tasks => Set<TaskEntity>();
    public DbSet<TaskComment> TaskComments => Set<TaskComment>();

    // Agents
    public DbSet<Agent> Agents => Set<Agent>();

    // MCP & Skills
    public DbSet<Mcp> Mcps => Set<Mcp>();
    public DbSet<Skill> Skills => Set<Skill>();

    // Notifications
    public DbSet<Notification> Notifications => Set<Notification>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all configurations from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // Configure PostgreSQL extensions
        modelBuilder.HasPostgresExtension("uuid-ossp");
        modelBuilder.HasPostgresExtension("pg_trgm");
    }

    /// <inheritdoc />
    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    /// <inheritdoc />
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries<AuditableEntity>();

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTimeOffset.UtcNow;
                entry.Entity.UpdatedAt = DateTimeOffset.UtcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTimeOffset.UtcNow;
            }
        }
    }
}
