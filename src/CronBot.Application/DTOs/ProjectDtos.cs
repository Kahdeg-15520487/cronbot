using System;
using CronBot.Domain.Enums;

namespace CronBot.Application.DTOs;

/// <summary>
/// Data transfer object for creating a new project.
/// </summary>
public record CreateProjectRequest
{
    /// <summary>
    /// Name of the project.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// URL-friendly slug.
    /// </summary>
    public required string Slug { get; init; }

    /// <summary>
    /// Description (optional).
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Git mode (internal, external, import).
    /// </summary>
    public GitMode GitMode { get; init; } = GitMode.Internal;

    /// <summary>
    /// External git URL (if using external git).
    /// </summary>
    public string? ExternalGitUrl { get; init; }

    /// <summary>
    /// Autonomy level (0-3).
    /// </summary>
    public int AutonomyLevel { get; init; } = 1;

    /// <summary>
    /// Sandbox mode.
    /// </summary>
    public SandboxMode SandboxMode { get; init; } = SandboxMode.Standard;

    /// <summary>
    /// Template name (optional).
    /// </summary>
    public string? TemplateName { get; init; }

    /// <summary>
    /// Maximum number of agents.
    /// </summary>
    public int MaxAgents { get; init; } = 3;
}

/// <summary>
/// Data transfer object for updating a project.
/// </summary>
public record UpdateProjectRequest
{
    /// <summary>
    /// Name of the project.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Autonomy level (0-3).
    /// </summary>
    public int? AutonomyLevel { get; init; }

    /// <summary>
    /// Sandbox mode.
    /// </summary>
    public SandboxMode? SandboxMode { get; init; }

    /// <summary>
    /// Maximum number of agents.
    /// </summary>
    public int? MaxAgents { get; init; }

    /// <summary>
    /// Whether the project is active.
    /// </summary>
    public bool? IsActive { get; init; }
}

/// <summary>
/// Data transfer object for project response.
/// </summary>
public record ProjectResponse
{
    /// <summary>
    /// Project ID.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Team ID.
    /// </summary>
    public Guid TeamId { get; init; }

    /// <summary>
    /// Name of the project.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// URL-friendly slug.
    /// </summary>
    public required string Slug { get; init; }

    /// <summary>
    /// Description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Git mode.
    /// </summary>
    public GitMode GitMode { get; init; }

    /// <summary>
    /// Internal repository URL.
    /// </summary>
    public string? InternalRepoUrl { get; init; }

    /// <summary>
    /// Autonomy level (0-3).
    /// </summary>
    public int AutonomyLevel { get; init; }

    /// <summary>
    /// Sandbox mode.
    /// </summary>
    public SandboxMode SandboxMode { get; init; }

    /// <summary>
    /// Maximum number of agents.
    /// </summary>
    public int MaxAgents { get; init; }

    /// <summary>
    /// Whether the project is active.
    /// </summary>
    public bool IsActive { get; init; }

    /// <summary>
    /// When the project was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }
}
