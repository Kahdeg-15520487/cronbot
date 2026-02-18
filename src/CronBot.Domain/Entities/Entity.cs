namespace CronBot.Domain.Entities;

/// <summary>
/// Base class for all entities with a GUID primary key.
/// </summary>
public abstract class Entity
{
    /// <summary>
    /// Unique identifier for the entity.
    /// </summary>
    public Guid Id { get; protected set; } = Guid.NewGuid();
}
