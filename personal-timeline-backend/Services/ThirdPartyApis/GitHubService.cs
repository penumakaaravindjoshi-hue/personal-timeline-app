using personal_timeline_backend.Data;
using personal_timeline_backend.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using System.Text.Json;
using System;
using System.Linq;
using Microsoft.Extensions.Logging; // Added for ILogger

namespace personal_timeline_backend.Services.ThirdPartyApis
{
    public class GitHubService : IThirdPartyApiService
    {
        private readonly TimelineContext _dbContext;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly ILogger<GitHubService> _logger; // Added for logging

        public GitHubService(TimelineContext dbContext, IConfiguration configuration, HttpClient httpClient, ILogger<GitHubService> logger)
        {
            _dbContext = dbContext;
            _configuration = configuration;
            _httpClient = httpClient;
            _logger = logger; // Initialize logger
        }

        public Task<bool> ConnectApiAsync(int userId, string apiProvider, string accessToken)
        {
            // This will be handled by the OAuth middleware's OnCreatingTicket event
            return Task.FromResult(true);
        }

        public async Task<bool> DisconnectApiAsync(int userId, string apiProvider)
        {
            var connection = await _dbContext.ApiConnections
                .FirstOrDefaultAsync(ac => ac.UserId == userId && ac.ApiProvider == apiProvider);

            if (connection != null)
            {
                connection.IsActive = false;
                await _dbContext.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task<IEnumerable<TimelineEntry>> SyncUserDataAsync(int userId, string apiProvider)
        {
            if (!string.Equals(apiProvider, "GitHub", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("This service only handles GitHub integration.");
            }

            var connection = await _dbContext.ApiConnections
                .FirstOrDefaultAsync(ac => ac.UserId == userId && ac.ApiProvider == "GitHub" && ac.IsActive);

            if (connection == null)
            {
                _logger.LogWarning("GitHub connection not found or inactive for user {UserId}", userId);
                return Enumerable.Empty<TimelineEntry>();
            }

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", connection.AccessToken);
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("personal-timeline-backend-app"); // Required by GitHub API

            // First, get the authenticated user's login (username)
            string githubUsername;
            try
            {
                var userResponse = await _httpClient.GetAsync("https://api.github.com/user");
                userResponse.EnsureSuccessStatusCode();
                var userContent = await userResponse.Content.ReadAsStringAsync();
                using (var userDoc = JsonDocument.Parse(userContent))
                {
                    githubUsername = userDoc.RootElement.GetProperty("login").GetString()!;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting GitHub username for user {UserId}", userId);
                throw new HttpRequestException($"Failed to get GitHub username: {ex.Message}", ex);
            }

            HttpResponseMessage response;
            try
            {
                // Now use the username to get events
                response = await _httpClient.GetAsync($"https://api.github.com/users/{githubUsername}/events");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error making HTTP request to GitHub API for user {UserId}", userId);
                throw new HttpRequestException($"Failed to connect to GitHub API: {ex.Message}", ex);
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("GitHub API request failed for user {UserId}. Status: {StatusCode}, Content: {Content}",
                    userId, response.StatusCode, errorContent);
                throw new HttpRequestException($"GitHub API returned non-success status code: {response.StatusCode}. Content: {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            using (var doc = JsonDocument.Parse(content))
            {
                var newEntries = new List<TimelineEntry>();

                foreach (var item in doc.RootElement.EnumerateArray())
                {
                    // Safely get properties, handling potential missing properties
                    string eventType = item.TryGetProperty("type", out var typeProp) ? typeProp.GetString() ?? string.Empty : string.Empty;
                    string eventId = item.TryGetProperty("id", out var idProp) ? idProp.GetString() ?? string.Empty : string.Empty;
                    DateTimeOffset createdAt = item.TryGetProperty("created_at", out var createdAtProp) && createdAtProp.ValueKind == JsonValueKind.String
                        ? createdAtProp.GetDateTimeOffset()
                        : DateTimeOffset.UtcNow;
                    
                    string repoName = string.Empty;
                    if (item.TryGetProperty("repo", out var repoProp) && repoProp.TryGetProperty("name", out var repoNameProp))
                    {
                        repoName = repoNameProp.GetString() ?? string.Empty;
                    }
                    
                    string externalUrl = $"https://github.com/{repoName}/activity"; // Generic activity URL

                    string title = string.Empty;
                    string description = string.Empty;
                    string entryType = "Activity";
                    string category = "Development";
                    string externalId = $"{eventId}-{repoName}"; // Unique ID for this event in this repo

                    // Check for duplicates
                    var existingEntry = await _dbContext.TimelineEntries.FirstOrDefaultAsync(
                        e => e.UserId == userId &&
                             e.SourceApi == "GitHub" &&
                             e.ExternalId == externalId
                    );

                    if (existingEntry != null)
                    {
                        continue; // Skip if already exists
                    }

                    switch (eventType)
                    {
                        case "PushEvent":
                            if (item.TryGetProperty("payload", out var payloadPush))
                            {
                                var refString = payloadPush.TryGetProperty("ref", out var refProp) ? refProp.GetString() : null;
                                var branchName = refString?.Split('/').Last() ?? "unknown branch";
                                var commitsCount = payloadPush.TryGetProperty("commits", out var commitsProp) ? commitsProp.GetArrayLength() : 0;
                                title = $"Pushed {commitsCount} commit(s) to {repoName} on {branchName}";
                                
                                if (payloadPush.TryGetProperty("head_commit", out var headCommitProp))
                                {
                                    description = headCommitProp.TryGetProperty("message", out var msgProp) ? msgProp.GetString() : string.Empty;
                                    externalUrl = headCommitProp.TryGetProperty("url", out var urlProp) ? urlProp.GetString() : externalUrl;
                                }
                            }
                            break;
                        case "CreateEvent":
                            if (item.TryGetProperty("payload", out var payloadCreate))
                            {
                                var refType = payloadCreate.TryGetProperty("ref_type", out var refTypeProp) ? refTypeProp.GetString() : null;
                                if (refType == "repository")
                                {
                                    title = $"Created new repository {repoName}";
                                    description = payloadCreate.TryGetProperty("description", out var descProp) ? descProp.GetString() : string.Empty;
                                    externalUrl = $"https://github.com/{repoName}";
                                }
                                else
                                {
                                    continue; // Ignore other create events for now (e.g., branch, tag)
                                }
                            }
                            break;
                        case "PullRequestEvent":
                            if (item.TryGetProperty("payload", out var payloadPR))
                            {
                                var action = payloadPR.TryGetProperty("action", out var actionProp) ? actionProp.GetString() : string.Empty;
                                var prNumber = payloadPR.TryGetProperty("number", out var numProp) ? numProp.GetInt32() : 0;
                                if (payloadPR.TryGetProperty("pull_request", out var prProp))
                                {
                                    var prTitle = prProp.TryGetProperty("title", out var prTitleProp) ? prTitleProp.GetString() : string.Empty;
                                    title = $"Pull Request {action}: #{prNumber} {prTitle} in {repoName}";
                                    externalUrl = prProp.TryGetProperty("html_url", out var htmlUrlProp) ? htmlUrlProp.GetString() : externalUrl;
                                }
                            }
                            break;
                        case "IssuesEvent":
                            if (item.TryGetProperty("payload", out var payloadIssue))
                            {
                                var issueAction = payloadIssue.TryGetProperty("action", out var issueActionProp) ? issueActionProp.GetString() : string.Empty;
                                var issueNumber = payloadIssue.TryGetProperty("number", out var issueNumProp) ? issueNumProp.GetInt32() : 0;
                                if (payloadIssue.TryGetProperty("issue", out var issueProp))
                                {
                                    var issueTitle = issueProp.TryGetProperty("title", out var issueTitleProp) ? issueTitleProp.GetString() : string.Empty;
                                    title = $"Issue {issueAction}: #{issueNumber} {issueTitle} in {repoName}";
                                    externalUrl = issueProp.TryGetProperty("html_url", out var htmlUrlProp) ? htmlUrlProp.GetString() : externalUrl;
                                }
                            }
                            break;
                        default:
                            _logger.LogInformation("Ignoring GitHub event type: {EventType} for user {UserId}", eventType, userId);
                            continue; // Ignore other event types for now
                    }

                    newEntries.Add(new TimelineEntry
                    {
                        UserId = userId,
                        Title = title,
                        Description = description,
                        EventDate = createdAt.DateTime,
                        EntryType = entryType,
                        Category = category,
                        ExternalUrl = externalUrl,
                        SourceApi = "GitHub",
                        ExternalId = externalId,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }

                if (newEntries.Any())
                {
                    _dbContext.TimelineEntries.AddRange(newEntries);
                    await _dbContext.SaveChangesAsync();
                }

                // Update LastSyncAt
                connection.LastSyncAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();

                return newEntries;
            }
        }

        public Task<IEnumerable<TimelineEntry>> GetRecentActivityAsync(int userId, ApiConnection connection)
        {
            // This method is not directly used by SyncUserDataAsync, but required by the interface.
            // SyncUserDataAsync handles fetching and processing.
            return Task.FromResult(Enumerable.Empty<TimelineEntry>());
        }

        public Task<bool> RefreshAccessTokenAsync(ApiConnection connection)
        {
            // TODO: GitHub refresh tokens are more complex and require enabling them in the OAuth app settings.
            // For now, we assume tokens are long-lived or the user will reconnect.
            return Task.FromResult(true);
        }
    }
}
