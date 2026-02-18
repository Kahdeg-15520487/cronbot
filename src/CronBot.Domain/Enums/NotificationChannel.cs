namespace CronBot.Domain.Enums;

/// <summary>
/// Channel for sending notifications.
/// </summary>
public enum NotificationChannel
{
    /// <summary>
    /// Telegram message.
    /// </summary>
    Telegram = 0,

    /// <summary>
    /// Web UI notification.
    /// </summary>
    Web = 1,

    /// <summary>
    /// Email notification.
    /// </summary>
    Email = 2
}
