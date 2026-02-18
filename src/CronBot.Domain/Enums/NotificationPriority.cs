namespace CronBot.Domain.Enums;

/// <summary>
/// Priority level for notifications.
/// </summary>
public enum NotificationPriority
{
    /// <summary>
    /// Low priority, informational only.
    /// </summary>
    Low = 0,

    /// <summary>
    /// Normal priority.
    /// </summary>
    Normal = 1,

    /// <summary>
    /// High priority, requires attention.
    /// </summary>
    High = 2,

    /// <summary>
    /// Urgent, requires immediate action.
    /// </summary>
    Urgent = 3
}
