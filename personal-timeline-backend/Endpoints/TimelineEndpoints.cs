using personal_timeline_backend.Data;
using personal_timeline_backend.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace personal_timeline_backend.Endpoints;

public static class TimelineEndpoints
{
    public static void MapTimelineEndpoints(this WebApplication app)
    {
        var timelineGroup = app.MapGroup("/timeline").RequireAuthorization();

        timelineGroup.MapGet("/", async (TimelineContext dbContext, ClaimsPrincipal principal) =>
        {
            var userId = int.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!); // Add !
            var entries = await dbContext.TimelineEntries
                .Where(e => e.UserId == userId)
                .OrderByDescending(e => e.EventDate)
                .ToListAsync();
            return Results.Ok(entries);
        });

        timelineGroup.MapGet("/{id}", async Task<Results<Ok<TimelineEntry>, NotFound>> (int id, TimelineContext dbContext, ClaimsPrincipal principal) =>
        {
            var userId = int.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!); // Add !
            var entry = await dbContext.TimelineEntries
                .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);

            return entry is TimelineEntry ? TypedResults.Ok(entry) : TypedResults.NotFound();
        });

        timelineGroup.MapPost("/", async (TimelineEntry entry, TimelineContext dbContext, ClaimsPrincipal principal) =>
        {
            var userId = int.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!); // Add !
            entry.UserId = userId;
            entry.CreatedAt = DateTime.UtcNow;
            entry.UpdatedAt = DateTime.UtcNow;

            dbContext.TimelineEntries.Add(entry);
            await dbContext.SaveChangesAsync();

            return TypedResults.Created($"/timeline/{entry.Id}", entry);
        });

        timelineGroup.MapPut("/{id}", async Task<Results<Ok<TimelineEntry>, NotFound, BadRequest>> (int id, TimelineEntry inputEntry, TimelineContext dbContext, ClaimsPrincipal principal) =>
        {
            if (id != inputEntry.Id)
            {
                return TypedResults.BadRequest();
            }

            var userId = int.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!); // Add !
            var entry = await dbContext.TimelineEntries
                .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);

            if (entry is null)
            {
                return TypedResults.NotFound();
            }

            entry.Title = inputEntry.Title;
            entry.Description = inputEntry.Description;
            entry.EventDate = inputEntry.EventDate;
            entry.EntryType = inputEntry.EntryType;
            entry.Category = inputEntry.Category;
            entry.ImageUrl = inputEntry.ImageUrl;
            entry.ExternalUrl = inputEntry.ExternalUrl;
            entry.UpdatedAt = DateTime.UtcNow;

            await dbContext.SaveChangesAsync();

            return TypedResults.Ok(entry);
        });

        timelineGroup.MapDelete("/{id}", async Task<Results<NoContent, NotFound>> (int id, TimelineContext dbContext, ClaimsPrincipal principal) =>
        {
            var userId = int.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!); // Add !
            var entry = await dbContext.TimelineEntries
                .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);

            if (entry is null)
            {
                return TypedResults.NotFound();
            }

            dbContext.TimelineEntries.Remove(entry);
            await dbContext.SaveChangesAsync();

            return TypedResults.NoContent();
        });
    }
}
