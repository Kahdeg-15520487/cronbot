namespace CronBot.Domain.Enums;

/// <summary>
/// Type of a task.
/// </summary>
public enum TaskType
{
    /// <summary>
    /// Regular task.
    /// </summary>
    Task = 0,

    /// <summary>
    /// Bug fix task.
    /// </summary>
    Bug = 1,

    /// <summary>
    /// Blocking issue requiring attention.
    /// </summary>
    Blocker = 2,

    /// <summary>
    /// Idea or feature request.
    /// </summary>
    Idea = 3,

    /// <summary>
    /// Epic containing multiple sub-tasks.
    /// </summary>
    Epic = 4
}
