namespace CronBot.Domain.Enums;

/// <summary>
/// Scope of a skill's availability.
/// </summary>
public enum SkillScope
{
    /// <summary>
    /// Available system-wide.
    /// </summary>
    System = 0,

    /// <summary>
    /// Available to a specific team.
    /// </summary>
    Team = 1,

    /// <summary>
    /// Available to a specific project.
    /// </summary>
    Project = 2
}
