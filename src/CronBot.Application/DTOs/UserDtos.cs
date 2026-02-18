using System;

namespace CronBot.Application.DTOs;

/// <summary>
/// Data transfer object for creating a new user.
/// </summary>
public record CreateUserRequest
{
    /// <summary>
    /// Username for the new user.
    /// </summary>
    public required string Username { get; init; }

    /// <summary>
    /// Email address (optional).
    /// </summary>
    public string? Email { get; init; }

    /// <summary>
    /// Telegram ID (optional).
    /// </summary>
    public long? TelegramId { get; init; }

    /// <summary>
    /// Authentik ID from OIDC (optional).
    /// </summary>
    public string? AuthentikId { get; init; }

    /// <summary>
    /// Display name (optional).
    /// </summary>
    public string? DisplayName { get; init; }

    /// <summary>
    /// Avatar URL (optional).
    /// </summary>
    public string? AvatarUrl { get; init; }
}

/// <summary>
/// Data transfer object for updating a user.
/// </summary>
public record UpdateUserRequest
{
    /// <summary>
    /// Email address.
    /// </summary>
    public string? Email { get; init; }

    /// <summary>
    /// Display name.
    /// </summary>
    public string? DisplayName { get; init; }

    /// <summary>
    /// Avatar URL.
    /// </summary>
    public string? AvatarUrl { get; init; }

    /// <summary>
    /// Whether the user is active.
    /// </summary>
    public bool? IsActive { get; init; }
}

/// <summary>
/// Data transfer object for user response.
/// </summary>
public record UserResponse
{
    /// <summary>
    /// User ID.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Username.
    /// </summary>
    public required string Username { get; init; }

    /// <summary>
    /// Email address.
    /// </summary>
    public string? Email { get; init; }

    /// <summary>
    /// Telegram ID.
    /// </summary>
    public long? TelegramId { get; init; }

    /// <summary>
    /// Display name.
    /// </summary>
    public string? DisplayName { get; init; }

    /// <summary>
    /// Avatar URL.
    /// </summary>
    public string? AvatarUrl { get; init; }

    /// <summary>
    /// Whether the user is active.
    /// </summary>
    public bool IsActive { get; init; }

    /// <summary>
    /// When the user was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }
}
