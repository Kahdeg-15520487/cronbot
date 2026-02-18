namespace CronBot.Domain.Enums;

/// <summary>
/// Security sandbox mode for project containers.
/// </summary>
public enum SandboxMode
{
    /// <summary>
    /// Standard Docker isolation.
    /// </summary>
    Standard = 0,

    /// <summary>
    /// Hardened with additional restrictions.
    /// </summary>
    Hardened = 1,

    /// <summary>
    /// Maximum security with minimal permissions.
    /// </summary>
    Maximum = 2
}
