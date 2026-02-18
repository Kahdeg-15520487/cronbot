namespace CronBot.Domain.Enums;

/// <summary>
/// Git repository mode for a project.
/// </summary>
public enum GitMode
{
    /// <summary>
    /// Uses internal Gitea repository.
    /// </summary>
    Internal = 0,

    /// <summary>
    /// Uses external Git provider (GitHub, GitLab, etc.).
    /// </summary>
    External = 1,

    /// <summary>
    /// Import from external source to internal repository.
    /// </summary>
    Import = 2
}
