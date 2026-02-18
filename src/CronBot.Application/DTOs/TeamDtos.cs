using System;

namespace CronBot.Application.DTOs;

/// <summary>
/// Data transfer object for creating a new team.
/// </summary>
public record CreateTeamRequest
{
    /// <summary>
    /// Name of the team.
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
    /// Whether this is a personal team.
    /// </summary>
    public bool IsPersonal { get; init; }
}

/// <summary>
/// Data transfer object for updating a team.
/// </summary>
public record UpdateTeamRequest
{
    /// <summary>
    /// Name of the team.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Description.
    /// </summary>
    public string? Description { get; init; }
}

/// <summary>
/// Data transfer object for team response.
/// </summary>
public record TeamResponse
{
    /// <summary>
    /// Team ID.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Name of the team.
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
    /// Whether this is a personal team.
    /// </summary>
    public bool IsPersonal { get; init; }

    /// <summary>
    /// Owner ID.
    /// </summary>
    public Guid OwnerId { get; init; }

    /// <summary>
    /// When the team was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }
}

/// <summary>
/// Data transfer object for adding a team member.
/// </summary>
public record AddTeamMemberRequest
{
    /// <summary>
    /// User ID to add.
    /// </summary>
    public required Guid UserId { get; init; }

    /// <summary>
    /// Role for the member.
    /// </summary>
    public string Role { get; init; } = "member";
}
