namespace CronBot.Domain.Enums;

/// <summary>
/// Role of a user within a team.
/// </summary>
public enum TeamRole
{
    /// <summary>
    /// Guest with limited read-only access.
    /// </summary>
    Guest = 0,

    /// <summary>
    /// Regular team member with standard permissions.
    /// </summary>
    Member = 1,

    /// <summary>
    /// Team administrator with management capabilities.
    /// </summary>
    Admin = 2,

    /// <summary>
    /// Team owner with full control.
    /// </summary>
    Owner = 3
}
