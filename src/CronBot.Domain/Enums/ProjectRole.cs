namespace CronBot.Domain.Enums;

/// <summary>
/// Role of a user within a project.
/// </summary>
public enum ProjectRole
{
    /// <summary>
    /// Read-only access to project resources.
    /// </summary>
    Reader = 0,

    /// <summary>
    /// Can contribute code and update tasks.
    /// </summary>
    Contributor = 1,

    /// <summary>
    /// Can plan sprints and manage backlog.
    /// </summary>
    Planner = 2,

    /// <summary>
    /// Full project administration.
    /// </summary>
    Admin = 3
}
