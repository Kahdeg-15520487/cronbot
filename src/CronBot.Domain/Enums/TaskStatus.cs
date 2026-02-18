namespace CronBot.Domain.Enums;

/// <summary>
/// Status of a task in the workflow.
/// </summary>
public enum TaskStatus
{
    /// <summary>
    /// In backlog, not yet scheduled.
    /// </summary>
    Backlog = 0,

    /// <summary>
    /// Added to sprint, waiting to start.
    /// </summary>
    Sprint = 1,

    /// <summary>
    /// Currently being worked on.
    /// </summary>
    InProgress = 2,

    /// <summary>
    /// In review, awaiting approval.
    /// </summary>
    Review = 3,

    /// <summary>
    /// Blocked by dependency or issue.
    /// </summary>
    Blocked = 4,

    /// <summary>
    /// Completed successfully.
    /// </summary>
    Done = 5,

    /// <summary>
    /// Cancelled or no longer needed.
    /// </summary>
    Cancelled = 6
}
