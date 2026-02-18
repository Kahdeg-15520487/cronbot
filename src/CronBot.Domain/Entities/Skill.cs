using CronBot.Domain.Enums;

namespace CronBot.Domain.Entities;

/// <summary>
/// Represents a skill (Python script) available to agents.
/// </summary>
public class Skill : AuditableEntity
{
    /// <summary>
    /// Name of the skill.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Version of the skill.
    /// </summary>
    public string Version { get; set; } = "1.0.0";

    /// <summary>
    /// Description of the skill.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Scope of the skill's availability.
    /// </summary>
    public SkillScope Scope { get; set; } = SkillScope.System;

    /// <summary>
    /// ID of the team (for team skills).
    /// </summary>
    public Guid? TeamId { get; set; }

    /// <summary>
    /// ID of the project (for project skills).
    /// </summary>
    public Guid? ProjectId { get; set; }

    /// <summary>
    /// Path to the skill file.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Skill metadata as JSON.
    /// </summary>
    public string Meta { get; set; } = "{}";

    /// <summary>
    /// Whether the skill is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public Team? Team { get; set; }
    public Project? Project { get; set; }
}
