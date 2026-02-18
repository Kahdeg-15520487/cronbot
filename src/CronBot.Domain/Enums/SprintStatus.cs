namespace CronBot.Domain.Enums;

/// <summary>
/// Status of a sprint.
/// </summary>
public enum SprintStatus
{
    /// <summary>
    /// Sprint is being planned.
    /// </summary>
    Planning = 0,

    /// <summary>
    /// Sprint is active and in progress.
    /// </summary>
    Active = 1,

    /// <summary>
    /// Sprint is completed, in review.
    /// </summary>
    Review = 2,

    /// <summary>
    /// Sprint is closed.
    /// </summary>
    Closed = 3
}
