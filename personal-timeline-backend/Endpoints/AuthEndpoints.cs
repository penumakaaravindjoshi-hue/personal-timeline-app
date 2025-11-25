using System.Security.Claims;
using personal_timeline_backend.Data;
using personal_timeline_backend.Models;
using personal_timeline_backend.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace personal_timeline_backend.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var authGroup = app.MapGroup("/auth");

        authGroup.MapGet("/google", ([FromQuery] string? returnUrl) =>
        {
            // Default to a frontend callback URL if not provided
            returnUrl ??= "https://localhost:5173/auth/callback"; // <--- REPLACE WITH YOUR ACTUAL FRONTEND CALLBACK URL
            var properties = new AuthenticationProperties { RedirectUri = returnUrl };
            return Results.Challenge(properties, new[] { GoogleDefaults.AuthenticationScheme });
        });

        authGroup.MapGet("/github", ([FromQuery] string? returnUrl, ClaimsPrincipal principal) =>
        {
            // Default to a frontend callback URL if not provided
            returnUrl ??= "https://localhost:5173/settings/api-connections"; // Redirect back to the connections page
            var properties = new AuthenticationProperties { RedirectUri = returnUrl };
            
            // Store the current user's ID in the properties so it can be retrieved in OnCreatingTicket
            var currentUserId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(currentUserId))
            {
                properties.Items["UserId"] = currentUserId;
            }

            return Results.Challenge(properties, new[] { "GitHub" });
        }).RequireAuthorization(new AuthorizeAttribute { AuthenticationSchemes = $"{CookieAuthenticationDefaults.AuthenticationScheme},{JwtBearerDefaults.AuthenticationScheme}" });

        authGroup.MapGet("/notion", ([FromQuery] string? returnUrl, ClaimsPrincipal principal) =>
        {
            returnUrl ??= "https://localhost:5173/settings/api-connections";
            var properties = new AuthenticationProperties { RedirectUri = returnUrl };
            
            var currentUserId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(currentUserId))
            {
                properties.Items["UserId"] = currentUserId;
            }

            return Results.Challenge(properties, new[] { "Notion" });
        }).RequireAuthorization(new AuthorizeAttribute { AuthenticationSchemes = $"{CookieAuthenticationDefaults.AuthenticationScheme},{JwtBearerDefaults.AuthenticationScheme}" });

        authGroup.MapGet("/me", (ClaimsPrincipal principal, TimelineContext dbContext, TokenService tokenService) =>
        {
            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = dbContext.Users.FirstOrDefault(u => u.Id.ToString() == userId);
            if (user == null)
            {
                return Results.NotFound();
            }

            var token = tokenService.GenerateToken(user);
            return Results.Ok(new { User = user, Token = token });

        }).RequireAuthorization(new AuthorizeAttribute { AuthenticationSchemes = $"{CookieAuthenticationDefaults.AuthenticationScheme},{JwtBearerDefaults.AuthenticationScheme}" });

        authGroup.MapPost("/logout", async (HttpContext httpContext) =>
        {
            await httpContext.SignOutAsync();
            return Results.Ok();
        });
    }
}