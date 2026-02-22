using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using CronBot.Infrastructure.Data;
using CronBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace CronBot.Infrastructure.Services;

/// <summary>
/// Service for authentication operations including password hashing and JWT token generation.
/// </summary>
public class AuthService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;
    private readonly string _jwtSecret;
    private readonly string _jwtIssuer;
    private readonly string _jwtAudience;
    private readonly int _jwtExpirationHours;

    public AuthService(
        AppDbContext context,
        IConfiguration configuration,
        ILogger<AuthService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;

        _jwtSecret = configuration["Jwt:Secret"] ?? "YourSuperSecretKeyThatIsAtLeast32CharactersLong!";
        _jwtIssuer = configuration["Jwt:Issuer"] ?? "CronBot";
        _jwtAudience = configuration["Jwt:Audience"] ?? "CronBot";
        _jwtExpirationHours = int.Parse(configuration["Jwt:ExpirationHours"] ?? "24");
    }

    /// <summary>
    /// Hash a password using BCrypt.
    /// </summary>
    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
    }

    /// <summary>
    /// Verify a password against a hash.
    /// </summary>
    public bool VerifyPassword(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }

    /// <summary>
    /// Generate a JWT token for a user.
    /// </summary>
    public string GenerateJwtToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (!string.IsNullOrEmpty(user.Email))
        {
            claims.Add(new Claim(ClaimTypes.Email, user.Email));
        }

        if (!string.IsNullOrEmpty(user.DisplayName))
        {
            claims.Add(new Claim("displayName", user.DisplayName));
        }

        var token = new JwtSecurityToken(
            issuer: _jwtIssuer,
            audience: _jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(_jwtExpirationHours),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Validate a JWT token and return the user ID.
    /// </summary>
    public Guid? ValidateToken(string token)
    {
        try
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret));
            var handler = new JwtSecurityTokenHandler();

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _jwtIssuer,
                ValidAudience = _jwtAudience,
                IssuerSigningKey = key,
                ClockSkew = TimeSpan.Zero
            };

            var principal = handler.ValidateToken(token, validationParameters, out var validatedToken);
            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return userId;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to validate JWT token");
            return null;
        }
    }

    /// <summary>
    /// Register a new user.
    /// </summary>
    public async Task<(User? User, string? Error)> RegisterAsync(
        string username,
        string password,
        string? email = null,
        string? displayName = null)
    {
        // Check if username already exists
        if (await _context.Users.AnyAsync(u => u.Username.ToLower() == username.ToLower()))
        {
            return (null, "Username already exists");
        }

        // Check if email already exists (if provided)
        if (!string.IsNullOrEmpty(email) && await _context.Users.AnyAsync(u => u.Email != null && u.Email.ToLower() == email.ToLower()))
        {
            return (null, "Email already exists");
        }

        var user = new User
        {
            Username = username,
            Email = email,
            DisplayName = displayName ?? username,
            PasswordHash = HashPassword(password),
            IsActive = true
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Registered new user: {Username}", username);

        return (user, null);
    }

    /// <summary>
    /// Authenticate a user and return a JWT token.
    /// </summary>
    public async Task<(string? Token, User? User, string? Error)> LoginAsync(string username, string password)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());

        if (user == null)
        {
            return (null, null, "Invalid username or password");
        }

        if (!user.IsActive)
        {
            return (null, null, "Account is disabled");
        }

        if (string.IsNullOrEmpty(user.PasswordHash) || !VerifyPassword(password, user.PasswordHash))
        {
            return (null, null, "Invalid username or password");
        }

        var token = GenerateJwtToken(user);

        _logger.LogInformation("User logged in: {Username}", username);

        return (token, user, null);
    }

    /// <summary>
    /// Get a user by ID.
    /// </summary>
    public async Task<User?> GetUserByIdAsync(Guid userId)
    {
        return await _context.Users.FindAsync(userId);
    }

    /// <summary>
    /// Get a user by username.
    /// </summary>
    public async Task<User?> GetUserByUsernameAsync(string username)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());
    }
}
