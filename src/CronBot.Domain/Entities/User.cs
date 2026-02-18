using CronBot.Domain.Enums;

namespace CronBot.Domain.Entities;

/// <summary>
/// Represents a user in the system.
/// </summary>
public class User : AuditableEntity
{
    /// <summary>
    /// Unique username for the user.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Email address (optional).
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Telegram user ID for bot interactions.
    /// </summary>
    public long? TelegramId { get; set; }

    /// <summary>
    /// Authentik identity provider ID.
    /// </summary>
    public string? AuthentikId { get; set; }

    /// <summary>
    /// Display name for the user.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// URL to user's avatar image.
    /// </summary>
    public string? AvatarUrl { get; set; }

    /// <summary>
    /// Whether the user account is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ICollection<TeamMember> TeamMemberships { get; set; } = [];
    public ICollection<ProjectMember> ProjectMemberships { get; set; } = [];
    public ICollection<Notification> Notifications { get; set; } = [];
}
