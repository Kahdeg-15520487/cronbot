using CronBot.Domain.Enums;

namespace CronBot.Domain.Entities;

/// <summary>
/// Represents a notification for a user.
/// </summary>
public class Notification : Entity
{
    /// <summary>
    /// ID of the user to notify.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// ID of the related project (optional).
    /// </summary>
    public Guid? ProjectId { get; set; }

    /// <summary>
    /// ID of the related task (optional).
    /// </summary>
    public Guid? TaskId { get; set; }

    /// <summary>
    /// ID of the related agent (optional).
    /// </summary>
    public Guid? AgentId { get; set; }

    /// <summary>
    /// Type of notification.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Title of the notification.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Message content.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Additional data as JSON.
    /// </summary>
    public string Data { get; set; } = "{}";

    /// <summary>
    /// Channel for delivery.
    /// </summary>
    public NotificationChannel Channel { get; set; } = NotificationChannel.Web;

    /// <summary>
    /// Priority of the notification.
    /// </summary>
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;

    /// <summary>
    /// When the notification was sent.
    /// </summary>
    public DateTimeOffset? SentAt { get; set; }

    /// <summary>
    /// When the notification was read.
    /// </summary>
    public DateTimeOffset? ReadAt { get; set; }

    /// <summary>
    /// When the notification was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation properties
    public User User { get; set; } = null!;
    public Project? Project { get; set; }
}
