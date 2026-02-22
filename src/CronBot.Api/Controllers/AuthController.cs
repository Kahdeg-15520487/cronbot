using CronBot.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace CronBot.Api.Controllers;

/// <summary>
/// Controller for authentication operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(AuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Register a new user.
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest("Username and password are required");
        }

        if (request.Password.Length < 6)
        {
            return BadRequest("Password must be at least 6 characters");
        }

        var (user, error) = await _authService.RegisterAsync(
            request.Username,
            request.Password,
            request.Email,
            request.DisplayName);

        if (error != null)
        {
            return BadRequest(error);
        }

        var token = _authService.GenerateJwtToken(user!);

        return Ok(new AuthResponse
        {
            Token = token,
            User = new AuthUserResponse
            {
                Id = user!.Id,
                Username = user.Username,
                Email = user.Email,
                DisplayName = user.DisplayName,
                AvatarUrl = user.AvatarUrl
            }
        });
    }

    /// <summary>
    /// Login with username and password.
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Unauthorized("Username and password are required");
        }

        var (token, user, error) = await _authService.LoginAsync(request.Username, request.Password);

        if (error != null)
        {
            return Unauthorized(error);
        }

        return Ok(new AuthResponse
        {
            Token = token!,
            User = new AuthUserResponse
            {
                Id = user!.Id,
                Username = user.Username,
                Email = user.Email,
                DisplayName = user.DisplayName,
                AvatarUrl = user.AvatarUrl
            }
        });
    }

    /// <summary>
    /// Get current user info from token.
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(AuthUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthUserResponse>> GetCurrentUser()
    {
        // Extract user ID from Authorization header
        var authHeader = Request.Headers.Authorization.FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            return Unauthorized("Missing or invalid authorization header");
        }

        var token = authHeader.Substring("Bearer ".Length);
        var userId = _authService.ValidateToken(token);

        if (userId == null)
        {
            return Unauthorized("Invalid or expired token");
        }

        var user = await _authService.GetUserByIdAsync(userId.Value);
        if (user == null)
        {
            return Unauthorized("User not found");
        }

        return Ok(new AuthUserResponse
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            DisplayName = user.DisplayName,
            AvatarUrl = user.AvatarUrl
        });
    }

    /// <summary>
    /// Validate a token.
    /// </summary>
    [HttpPost("validate")]
    [ProducesResponseType(typeof(TokenValidationResponse), StatusCodes.Status200OK)]
    public ActionResult<TokenValidationResponse> ValidateToken([FromBody] ValidateTokenRequest request)
    {
        var userId = _authService.ValidateToken(request.Token);

        return Ok(new TokenValidationResponse
        {
            Valid = userId != null,
            UserId = userId
        });
    }
}

/// <summary>
/// Registration request.
/// </summary>
public record RegisterRequest
{
    /// <summary>
    /// Username for the new account.
    /// </summary>
    [Required]
    public string Username { get; init; } = string.Empty;

    /// <summary>
    /// Password for the new account.
    /// </summary>
    [Required]
    public string Password { get; init; } = string.Empty;

    /// <summary>
    /// Optional email address.
    /// </summary>
    public string? Email { get; init; }

    /// <summary>
    /// Optional display name.
    /// </summary>
    public string? DisplayName { get; init; }
}

/// <summary>
/// Login request.
/// </summary>
public record LoginRequest
{
    /// <summary>
    /// Username.
    /// </summary>
    [Required]
    public string Username { get; init; } = string.Empty;

    /// <summary>
    /// Password.
    /// </summary>
    [Required]
    public string Password { get; init; } = string.Empty;
}

/// <summary>
/// Authentication response with token and user info.
/// </summary>
public record AuthResponse
{
    /// <summary>
    /// JWT token for authentication.
    /// </summary>
    public string Token { get; init; } = string.Empty;

    /// <summary>
    /// User information.
    /// </summary>
    public AuthUserResponse User { get; init; } = null!;
}

/// <summary>
/// User response DTO for auth endpoints.
/// </summary>
public record AuthUserResponse
{
    /// <summary>
    /// User ID.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Username.
    /// </summary>
    public string Username { get; init; } = string.Empty;

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
}

/// <summary>
/// Token validation request.
/// </summary>
public record ValidateTokenRequest
{
    /// <summary>
    /// JWT token to validate.
    /// </summary>
    public string Token { get; init; } = string.Empty;
}

/// <summary>
/// Token validation response.
/// </summary>
public record TokenValidationResponse
{
    /// <summary>
    /// Whether the token is valid.
    /// </summary>
    public bool Valid { get; init; }

    /// <summary>
    /// User ID from the token (if valid).
    /// </summary>
    public Guid? UserId { get; init; }
}
