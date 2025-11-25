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
        var timelineGroup = app.MapGroup("/timeline").RequireAuthorization().WithTags("Timeline");

        /// <summary>
        /// Gets all timeline entries for the current user.
        /// </summary>
        /// <returns>A list of timeline entries, ordered by event date descending.</returns>
        /// <response code="200">Returns the list of timeline entries.</response>
        /// <response code="401">If the user is not authenticated.</response>
        timelineGroup.MapGet("/", async (TimelineContext dbContext, ClaimsPrincipal principal) =>
        {
            var userId = int.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var entries = await dbContext.TimelineEntries
                .Where(e => e.UserId == userId)
                .OrderByDescending(e => e.EventDate)
                .ToListAsync();
            return Results.Ok(entries);
        });

        /// <summary>
        /// Gets a specific timeline entry by its ID.
        /// </summary>
        /// <param name="id">The ID of the timeline entry to retrieve.</param>
        /// <returns>The requested timeline entry.</returns>
        /// <response code="200">Returns the requested timeline entry.</response>
        /// <response code="401">If the user is not authenticated.</response>
        /// <response code="404">If the timeline entry is not found.</response>
        timelineGroup.MapGet("/{id}", async Task<Results<Ok<TimelineEntry>, NotFound>> (int id, TimelineContext dbContext, ClaimsPrincipal principal) =>
        {
            var userId = int.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var entry = await dbContext.TimelineEntries
                .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);

            return entry is TimelineEntry ? TypedResults.Ok(entry) : TypedResults.NotFound();
        });

        /// <summary>
        /// Creates a new timeline entry.
        /// </summary>
        /// <param name="entry">The timeline entry to create. The ID, UserId, CreatedAt, and UpdatedAt properties will be set by the server.</param>
        /// <returns>The newly created timeline entry.</returns>
        /// <response code="201">Returns the newly created timeline entry.</response>
        /// <response code="400">If the entry is invalid.</response>
        /// <response code="401">If the user is not authenticated.</response>
        timelineGroup.MapPost("/", async (TimelineEntry entry, TimelineContext dbContext, ClaimsPrincipal principal) =>
        {
            var userId = int.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
            entry.UserId = userId;
            entry.CreatedAt = DateTime.UtcNow;
            entry.UpdatedAt = DateTime.UtcNow;

            dbContext.TimelineEntries.Add(entry);
            await dbContext.SaveChangesAsync();

            return TypedResults.Created($"/timeline/{entry.Id}", entry);
        });

        /// <summary>
        /// Updates an existing timeline entry.
        /// </summary>
        /// <param name="id">The ID of the timeline entry to update.</param>
        /// <param name="inputEntry">The updated timeline entry data.</param>
        /// <returns>The updated timeline entry.</returns>
        /// <response code="200">Returns the updated timeline entry.</response>
        /// <response code="400">If the input entry's ID does not match the route ID.</response>
        /// <response code="401">If the user is not authenticated.</response>
        /// <response code="404">If the timeline entry is not found.</response>
        timelineGroup.MapPut("/{id}", async Task<Results<Ok<TimelineEntry>, NotFound, BadRequest>> (int id, TimelineEntry inputEntry, TimelineContext dbContext, ClaimsPrincipal principal) =>
        {
            if (id != inputEntry.Id)
            {
                return TypedResults.BadRequest();
            }

            var userId = int.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
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

        /// <summary>
        /// Deletes a timeline entry.
        /// </summary>
        /// <param name="id">The ID of the timeline entry to delete.</param>
        /// <returns>No content.</returns>
        /// <response code="204">If the entry was successfully deleted.</response>
        /// <response code="401">If the user is not authenticated.</response>
        /// <response code="404">If the timeline entry is not found.</response>
        timelineGroup.MapDelete("/{id}", async Task<Results<NoContent, NotFound>> (int id, TimelineContext dbContext, ClaimsPrincipal principal) =>
        {
            var userId = int.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
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