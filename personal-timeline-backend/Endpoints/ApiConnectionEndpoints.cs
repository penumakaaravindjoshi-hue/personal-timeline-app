using System.Security.Claims;
using personal_timeline_backend.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using personal_timeline_backend.Services; // Added for IThirdPartyApiService
using Microsoft.AspNetCore.Http.HttpResults; // Added for TypedResults
using personal_timeline_backend.Models; // Added for TimelineEntry
using Microsoft.Extensions.DependencyInjection; // Added for IServiceProvider
using Microsoft.AspNetCore.Http; // Added for IHttpContextAccessor

namespace personal_timeline_backend.Endpoints;

public static class ApiConnectionEndpoints
{
    public static void MapApiConnectionEndpoints(this WebApplication app)
    {
        var apiGroup = app.MapGroup("/api/connections").RequireAuthorization();

        apiGroup.MapGet("/", async (TimelineContext dbContext, ClaimsPrincipal principal) =>
        {
            var userId = int.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var connections = await dbContext.ApiConnections
                .Where(ac => ac.UserId == userId)
                .Select(ac => new { ac.ApiProvider, ac.IsActive, ac.LastSyncAt }) // Include LastSyncAt
                .ToListAsync();
            return Results.Ok(connections);
        });

        apiGroup.MapDelete("/{provider}", async (string provider, TimelineContext dbContext, ClaimsPrincipal principal) =>
        {
            var userId = int.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var connection = await dbContext.ApiConnections
                .FirstOrDefaultAsync(ac => ac.UserId == userId && ac.ApiProvider.ToLower() == provider.ToLower());

            if (connection == null)
            {
                return Results.NotFound();
            }

            connection.IsActive = false;
            await dbContext.SaveChangesAsync();

            return Results.NoContent();
        });

        apiGroup.MapPost("/{provider}/sync", SyncApiHandler);
    }

    // Static local function to handle the sync logic
    private static async Task<IResult> SyncApiHandler(
        string provider,
        IServiceProvider serviceProvider // Inject IServiceProvider
    )
    {
        // Resolve services manually
        var dbContext = serviceProvider.GetRequiredService<TimelineContext>();
        var httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();
        var principal = httpContextAccessor.HttpContext!.User;

        var userId = int.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
        
        // Resolve all IThirdPartyApiService implementations
        var apiServices = serviceProvider.GetServices<IThirdPartyApiService>();
        var service = apiServices.FirstOrDefault(s => s.GetType().Name.Contains(provider, StringComparison.OrdinalIgnoreCase));

        if (service == null)
        {
            return TypedResults.NotFound($"No service found for provider: {provider}");
        }

        try
        {
            var newEntries = await service.SyncUserDataAsync(userId, provider);
            return TypedResults.Ok(newEntries);
        }
        catch (ArgumentException ex)
        {
            return TypedResults.BadRequest(ex.Message);
        }
        catch (HttpRequestException ex)
        {
            // Handle API call errors (e.g., token expired, rate limit)
            return TypedResults.BadRequest($"API call failed for {provider}: {ex.Message}");
        }
        catch (Exception ex)
        {
            // General error
            return TypedResults.BadRequest($"An unexpected error occurred during sync for {provider}: {ex.Message}");
        }
    }
}
