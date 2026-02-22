using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CronBot.Infrastructure.Services;

/// <summary>
/// Service for interacting with Gitea API to manage repositories.
/// </summary>
public class GitService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GitService> _logger;
    private readonly string _giteaUrl;
    private readonly string _giteaToken;
    private readonly string _giteaUsername;
    private readonly string _giteaPassword;
    private bool _isAuthenticated;

    public GitService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<GitService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("Gitea");
        _logger = logger;
        _giteaUrl = configuration["Gitea:Url"] ?? "http://gitea:3000";
        _giteaToken = configuration["Gitea:Token"] ?? "";
        _giteaUsername = configuration["Gitea:Username"] ?? "cronbot";
        _giteaPassword = configuration["Gitea:Password"] ?? "cronbot123";

        _httpClient.BaseAddress = new Uri(_giteaUrl);
        _isAuthenticated = false;
    }

    /// <summary>
    /// Ensure we have valid authentication (token or basic auth).
    /// </summary>
    private async Task EnsureAuthenticatedAsync(CancellationToken cancellationToken)
    {
        if (_isAuthenticated) return;

        // If we have a token, use it
        if (!string.IsNullOrEmpty(_giteaToken))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("token", _giteaToken);
            _isAuthenticated = true;
            _logger.LogDebug("Using Gitea token authentication");
            return;
        }

        // Otherwise, try to get a token using basic auth
        try
        {
            var basicAuth = Convert.ToBase64String(
                Encoding.ASCII.GetBytes($"{_giteaUsername}:{_giteaPassword}"));

            // Create a temporary request to get/create a token
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/users/{username}/tokens");
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", basicAuth);
            request.Content = JsonContent.Create(new { name = "cronbot-api-token" });

            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var tokenResponse = await response.Content.ReadFromJsonAsync<GiteaToken>(cancellationToken);
                if (tokenResponse?.Sha1 != null)
                {
                    // Now use token auth
                    _httpClient.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("token", tokenResponse.Sha1);
                    _isAuthenticated = true;
                    _logger.LogInformation("Created and cached Gitea API token");
                    return;
                }
            }

            // Fallback: use basic auth for all requests
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", basicAuth);
            _isAuthenticated = true;
            _logger.LogDebug("Using Gitea basic auth (fallback)");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to setup Gitea authentication");
        }
    }

    /// <summary>
    /// Create a new repository in Gitea.
    /// </summary>
    public async Task<GiteaRepo?> CreateRepoAsync(
        string name,
        string? description = null,
        bool isPrivate = true,
        CancellationToken cancellationToken = default)
    {
        await EnsureAuthenticatedAsync(cancellationToken);

        try
        {
            var request = new
            {
                name = SanitizeRepoName(name),
                description = description ?? $"CronBot project: {name}",
                @private = isPrivate,
                auto_init = true,
                default_branch = "main",
                gitignores = "",
                license = "",
                readme = "Default",
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"/api/v1/user/repos",
                request,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError(
                    "Failed to create Gitea repo {RepoName}: {StatusCode} - {Error}",
                    name, response.StatusCode, error);
                return null;
            }

            var repo = await response.Content.ReadFromJsonAsync<GiteaRepo>(cancellationToken);
            _logger.LogInformation("Created Gitea repo: {RepoName}", name);
            return repo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Gitea repo {RepoName}", name);
            return null;
        }
    }

    /// <summary>
    /// Get a repository by name.
    /// </summary>
    public async Task<GiteaRepo?> GetRepoAsync(
        string owner,
        string repoName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"/api/v1/repos/{owner}/{repoName}",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<GiteaRepo>(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting Gitea repo {Owner}/{RepoName}", owner, repoName);
            return null;
        }
    }

    /// <summary>
    /// Delete a repository.
    /// </summary>
    public async Task<bool> DeleteRepoAsync(
        string owner,
        string repoName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.DeleteAsync(
                $"/api/v1/repos/{owner}/{repoName}",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError(
                    "Failed to delete Gitea repo {Owner}/{RepoName}: {StatusCode} - {Error}",
                    owner, repoName, response.StatusCode, error);
                return false;
            }

            _logger.LogInformation("Deleted Gitea repo: {Owner}/{RepoName}", owner, repoName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting Gitea repo {Owner}/{RepoName}", owner, repoName);
            return false;
        }
    }

    /// <summary>
    /// Get the clone URL with embedded credentials for the agent.
    /// </summary>
    public string GetCloneUrl(string owner, string repoName, bool includeCredentials = false)
    {
        if (includeCredentials && !string.IsNullOrEmpty(_giteaToken))
        {
            // URL with token for authentication
            return $"https://oauth2:{_giteaToken}@{_giteaUrl.Replace("http://", "").Replace("https://", "")}/{owner}/{repoName}.git";
        }

        return $"{_giteaUrl}/{owner}/{repoName}.git";
    }

    /// <summary>
    /// Get the web URL for a repository.
    /// </summary>
    public string GetWebUrl(string owner, string repoName)
    {
        return $"{_giteaUrl}/{owner}/{repoName}";
    }

    /// <summary>
    /// Initialize a repository with a template.
    /// </summary>
    public Task<bool> InitializeFromTemplateAsync(
        string owner,
        string repoName,
        string template = "empty",
        CancellationToken cancellationToken = default)
    {
        // For now, repos are initialized with auto_init=true on creation
        // In the future, this could push template files
        _logger.LogInformation("Repository {Owner}/{RepoName} initialized with template: {Template}",
            owner, repoName, template);
        return Task.FromResult(true);
    }

    /// <summary>
    /// Sanitize repository name for Gitea (lowercase, alphanumeric, dashes).
    /// </summary>
    private static string SanitizeRepoName(string name)
    {
        var sanitized = name.ToLowerInvariant();
        sanitized = new string(sanitized.Select(c =>
            char.IsLetterOrDigit(c) ? c : '-').ToArray());
        sanitized = sanitized.Trim('-');
        return sanitized;
    }

    /// <summary>
    /// Create a pull request in Gitea.
    /// </summary>
    public async Task<GiteaPullRequest?> CreatePullRequestAsync(
        string owner,
        string repoName,
        string title,
        string headBranch,
        string baseBranch = "main",
        string? body = null,
        CancellationToken cancellationToken = default)
    {
        await EnsureAuthenticatedAsync(cancellationToken);

        try
        {
            var request = new
            {
                title,
                head = headBranch,
                @base = baseBranch,
                body = body ?? $"Pull request for branch: {headBranch}"
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"/api/v1/repos/{owner}/{repoName}/pulls",
                request,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError(
                    "Failed to create PR in {Owner}/{RepoName}: {StatusCode} - {Error}",
                    owner, repoName, response.StatusCode, error);
                return null;
            }

            var pr = await response.Content.ReadFromJsonAsync<GiteaPullRequest>(cancellationToken);
            _logger.LogInformation("Created PR #{PRNumber} in {Owner}/{RepoName}: {Title}",
                pr?.Number, owner, repoName, title);
            return pr;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating PR in {Owner}/{RepoName}", owner, repoName);
            return null;
        }
    }

    /// <summary>
    /// Get a pull request by number.
    /// </summary>
    public async Task<GiteaPullRequest?> GetPullRequestAsync(
        string owner,
        string repoName,
        int prNumber,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"/api/v1/repos/{owner}/{repoName}/pulls/{prNumber}",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<GiteaPullRequest>(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting PR #{PRNumber} in {Owner}/{RepoName}", prNumber, owner, repoName);
            return null;
        }
    }

    /// <summary>
    /// List pull requests for a repository.
    /// </summary>
    public async Task<List<GiteaPullRequest>> ListPullRequestsAsync(
        string owner,
        string repoName,
        string? state = "open",
        CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"/api/v1/repos/{owner}/{repoName}/pulls";
            if (!string.IsNullOrEmpty(state))
            {
                url += $"?state={state}";
            }

            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return new List<GiteaPullRequest>();
            }

            var prs = await response.Content.ReadFromJsonAsync<List<GiteaPullRequest>>(cancellationToken);
            return prs ?? new List<GiteaPullRequest>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error listing PRs in {Owner}/{RepoName}", owner, repoName);
            return new List<GiteaPullRequest>();
        }
    }

    /// <summary>
    /// Merge a pull request.
    /// </summary>
    public async Task<bool> MergePullRequestAsync(
        string owner,
        string repoName,
        int prNumber,
        string? mergeMessage = null,
        CancellationToken cancellationToken = default)
    {
        await EnsureAuthenticatedAsync(cancellationToken);

        try
        {
            var request = new
            {
                Do = "merge",
                MergeMessage = mergeMessage ?? $"Merge PR #{prNumber}"
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"/api/v1/repos/{owner}/{repoName}/pulls/{prNumber}/merge",
                request,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError(
                    "Failed to merge PR #{PRNumber} in {Owner}/{RepoName}: {StatusCode} - {Error}",
                    prNumber, owner, repoName, response.StatusCode, error);
                return false;
            }

            _logger.LogInformation("Merged PR #{PRNumber} in {Owner}/{RepoName}", prNumber, owner, repoName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error merging PR #{PRNumber} in {Owner}/{RepoName}", prNumber, owner, repoName);
            return false;
        }
    }

    /// <summary>
    /// Get the PR web URL.
    /// </summary>
    public string GetPullRequestWebUrl(string owner, string repoName, int prNumber)
    {
        return $"{_giteaUrl}/{owner}/{repoName}/pulls/{prNumber}";
    }
}

/// <summary>
/// Gitea repository response.
/// </summary>
public class GiteaRepo
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("full_name")]
    public string FullName { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("html_url")]
    public string HtmlUrl { get; set; } = string.Empty;

    [JsonPropertyName("clone_url")]
    public string CloneUrl { get; set; } = string.Empty;

    [JsonPropertyName("ssh_url")]
    public string SshUrl { get; set; } = string.Empty;

    [JsonPropertyName("owner")]
    public GiteaUser? Owner { get; set; }

    [JsonPropertyName("private")]
    public bool Private { get; set; }

    [JsonPropertyName("default_branch")]
    public string DefaultBranch { get; set; } = "main";
}

/// <summary>
/// Gitea user in repository response.
/// </summary>
public class GiteaUser
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("login")]
    public string Login { get; set; } = string.Empty;

    [JsonPropertyName("full_name")]
    public string? FullName { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }
}

/// <summary>
/// Gitea token response.
/// </summary>
public class GiteaToken
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("sha1")]
    public string? Sha1 { get; set; }

    [JsonPropertyName("token_last_eight")]
    public string? TokenLastEight { get; set; }
}

/// <summary>
/// Gitea pull request response.
/// </summary>
public class GiteaPullRequest
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("number")]
    public int Number { get; set; }

    [JsonPropertyName("state")]
    public string State { get; set; } = "open";

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("body")]
    public string? Body { get; set; }

    [JsonPropertyName("html_url")]
    public string HtmlUrl { get; set; } = string.Empty;

    [JsonPropertyName("mergeable")]
    public bool? Mergeable { get; set; }

    [JsonPropertyName("merged")]
    public bool Merged { get; set; }

    [JsonPropertyName("merged_at")]
    public DateTimeOffset? MergedAt { get; set; }

    [JsonPropertyName("head")]
    public GiteaPRBranch? Head { get; set; }

    [JsonPropertyName("base")]
    public GiteaPRBranch? Base { get; set; }

    [JsonPropertyName("user")]
    public GiteaUser? User { get; set; }

    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>
/// Gitea PR branch info.
/// </summary>
public class GiteaPRBranch
{
    [JsonPropertyName("ref")]
    public string Ref { get; set; } = string.Empty;

    [JsonPropertyName("sha")]
    public string Sha { get; set; } = string.Empty;

    [JsonPropertyName("repo")]
    public GiteaRepo? Repo { get; set; }
}
