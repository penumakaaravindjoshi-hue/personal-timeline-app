using personal_timeline_backend.Data;
using personal_timeline_backend.Models;
using Microsoft.EntityFrameworkCore; // Added this line
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using System.Text.Json;
using System;
using System.Linq;
using Microsoft.Extensions.Logging; // Added for ILogger

namespace personal_timeline_backend.Services.ThirdPartyApis
{
    public class NotionService : IThirdPartyApiService
    {
        private readonly TimelineContext _dbContext;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly ILogger<NotionService> _logger; // Added for logging

        public NotionService(TimelineContext dbContext, IConfiguration configuration, HttpClient httpClient, ILogger<NotionService> logger)
        {
            _dbContext = dbContext;
            _configuration = configuration;
            _httpClient = httpClient;
            _logger = logger; // Initialize logger
        }

        public Task<bool> ConnectApiAsync(int userId, string apiProvider, string accessToken)
        {
            // This will be handled by the OAuth middleware
            return Task.FromResult(true);
        }

        public Task<bool> DisconnectApiAsync(int userId, string apiProvider)
        {
            // Implementation to be added
            throw new NotImplementedException();
        }

        public Task<IEnumerable<TimelineEntry>> GetRecentActivityAsync(int userId, ApiConnection connection)
        {
            // Implementation to be added
            throw new NotImplementedException();
        }

        public Task<bool> RefreshAccessTokenAsync(ApiConnection connection)
        {
            // Notion refresh tokens are not supported, so this will likely remain empty
            return Task.FromResult(true);
        }

        public async Task<IEnumerable<TimelineEntry>> SyncUserDataAsync(int userId, string apiProvider)
        {
            _logger.LogInformation("Sync Notion data for user {UserId}", userId);

            if (apiProvider != "notion")
                throw new ArgumentException("This service only handles Notion integration.");

            var connection = await _dbContext.ApiConnections
                .FirstOrDefaultAsync(ac => ac.UserId == userId && ac.ApiProvider == "Notion" && ac.IsActive);

            if (connection == null)
                return Enumerable.Empty<TimelineEntry>();


            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", connection.AccessToken);

            _httpClient.DefaultRequestHeaders.Remove("Notion-Version");
            _httpClient.DefaultRequestHeaders.Add("Notion-Version", "2022-06-28");

            try
            {
                // Buscar databases
                var searchBody = new
                {
                    filter = new { value = "database", property = "object" },
                    page_size = 20
                };

                var searchJson = new StringContent(
                    JsonSerializer.Serialize(searchBody),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

                var searchResponse = await _httpClient.PostAsync("https://api.notion.com/v1/search", searchJson);
                var searchContent = await searchResponse.Content.ReadAsStringAsync();
                searchResponse.EnsureSuccessStatusCode();

                using var searchDoc = JsonDocument.Parse(searchContent);
                var databases = searchDoc.RootElement.GetProperty("results");

                List<TimelineEntry> newEntries = new();


                foreach (var db in databases.EnumerateArray())
                {
                    string databaseId = db.GetProperty("id").GetString();
                    _logger.LogInformation("Querying Notion database {DatabaseId}", databaseId);

                    var queryBody = new { page_size = 50 };
                    var queryJson = new StringContent(
                        JsonSerializer.Serialize(queryBody),
                        System.Text.Encoding.UTF8,
                        "application/json"
                    );

                    var queryResponse = await _httpClient.PostAsync(
                        $"https://api.notion.com/v1/databases/{databaseId}/query",
                        queryJson
                    );

                    var queryText = await queryResponse.Content.ReadAsStringAsync();
                    queryResponse.EnsureSuccessStatusCode();

                    using var queryDoc = JsonDocument.Parse(queryText);
                    var pages = queryDoc.RootElement.GetProperty("results");


                    foreach (var page in pages.EnumerateArray())
                    {
                        var props = page.GetProperty("properties");

                        // === TITLE ===
                        string title = "(Untitled)";
                        var titleProp = props.EnumerateObject()
                            .FirstOrDefault(p => p.Value.TryGetProperty("title", out _));

                        if (titleProp.Value.ValueKind != JsonValueKind.Undefined)
                        {
                            var arr = titleProp.Value.GetProperty("title").EnumerateArray();
                            if (arr.Any())
                                title = arr.First().GetProperty("plain_text").GetString();
                        }

                        // === DESCRIPTION (rich_text) ===
                        string description = null;
                        var descProp = props.EnumerateObject()
                            .FirstOrDefault(p => p.Value.TryGetProperty("rich_text", out _));

                        if (descProp.Value.ValueKind != JsonValueKind.Undefined)
                        {
                            var arr = descProp.Value.GetProperty("rich_text").EnumerateArray();
                            if (arr.Any())
                                description = arr.First().GetProperty("plain_text").GetString();
                        }

                        // === DATE ===
                        DateTime? createdAt = DateTime.UtcNow;
                        var dateProp = props.EnumerateObject()
                            .FirstOrDefault(p => p.Value.TryGetProperty("date", out _));

                        if (dateProp.Value.ValueKind != JsonValueKind.Undefined &&
                            dateProp.Value.GetProperty("date").TryGetProperty("start", out var dateValue))
                        {
                            if (DateTime.TryParse(dateValue.GetString(), out var parsedDate))
                                createdAt = parsedDate;
                        }

                        // === CATEGORY (select) ===
                        string category = null;
                        var selectProp = props.EnumerateObject()
                            .FirstOrDefault(p => p.Value.TryGetProperty("select", out _));

                        if (selectProp.Value.ValueKind != JsonValueKind.Undefined)
                        {
                            var sel = selectProp.Value.GetProperty("select");
                            if (sel.ValueKind != JsonValueKind.Null &&
                                sel.TryGetProperty("name", out var name))
                            {
                                category = name.GetString();
                            }
                        }

                        // === EXTERNAL URL ===
                        string externalUrl = null;
                        var urlProp = props.EnumerateObject()
                            .FirstOrDefault(p => p.Value.TryGetProperty("url", out _));

                        if (urlProp.Value.ValueKind != JsonValueKind.Undefined)
                        {
                            externalUrl = urlProp.Value.GetProperty("url").GetString();
                        }

                        // === EXTERNAL ID (el id de la página) ===
                        string externalId = page.GetProperty("id").GetString();

                        // === ENTRY TYPE ===
                        string entryType = "notion";

                        // === Crear entrada final ===
                        newEntries.Add(new TimelineEntry
                        {
                            UserId = userId,
                            Title = title,
                            Description = description,
                            EventDate = createdAt.Value,
                            EntryType = entryType,
                            Category = category,
                            ExternalUrl = externalUrl,
                            SourceApi = "Notion",
                            ExternalId = externalId,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        });
                    }
                }

                await _dbContext.TimelineEntries.AddRangeAsync(newEntries);
                await _dbContext.SaveChangesAsync();

                connection.LastSyncAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();

                return newEntries;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing Notion");
                throw;
            }
        }



    } // Closing brace for NotionService class
} // Closing brace for namespace personal_timeline_backend.Services.ThirdPartyApis
