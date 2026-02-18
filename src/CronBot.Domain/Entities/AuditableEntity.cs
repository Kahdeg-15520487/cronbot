namespace CronBot.Domain.Entities;

/// <summary>
/// Base class for entities with creation and modification timestamps.
/// </summary>
public abstract class AuditableEntity : Entity
{
    /// <summary>
    /// UTC timestamp when the entity was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// UTC timestamp when the entity was last updated.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
