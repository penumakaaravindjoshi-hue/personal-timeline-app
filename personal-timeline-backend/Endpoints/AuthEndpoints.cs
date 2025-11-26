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
        var authGroup = app.MapGroup("/auth").WithTags("Authentication");

        /// <summary>
        /// Initiates the Google OAuth2 login flow.
        /// </summary>
        /// <param name="returnUrl">Optional URL to redirect to after successful authentication.</param>
        /// <returns>A challenge result that redirects the user to Google's login page.</returns>
        authGroup.MapGet("/google", ([FromQuery] string? returnUrl) =>
        {
            // Default to a frontend callback URL if not provided
            returnUrl ??= "https://localhost:5173/auth/callback"; // <--- REPLACE WITH YOUR ACTUAL FRONTEND CALLBACK URL
            var properties = new AuthenticationProperties { RedirectUri = returnUrl };
            return Results.Challenge(properties, new[] { GoogleDefaults.AuthenticationScheme });
        });

        /// <summary>
        /// Initiates the GitHub OAuth2 flow to connect a user's GitHub account. Requires authentication.
        /// </summary>
        /// <param name="returnUrl">Optional URL to redirect to after successful connection.</param>
        /// <param name="principal">The authenticated user's principal.</param>
        /// <returns>A challenge result that redirects the user to GitHub's authorization page.</returns>
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

        /// <summary>
        /// Initiates the Notion OAuth2 flow to connect a user's Notion account. Requires authentication.
        /// </summary>
        /// <param name="returnUrl">Optional URL to redirect to after successful connection.</param>
        /// <param name="principal">The authenticated user's principal.</param>
        /// <returns>A challenge result that redirects the user to Notion's authorization page.</returns>
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

        /// <summary>
        /// Gets the current authenticated user's profile and a new JWT. Requires authentication.
        /// </summary>
        /// <returns>The user's profile information and a JWT.</returns>
        /// <response code="200">Returns the user's profile and a token.</response>
        /// <response code="404">If the user is not found in the database.</response>
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

        /// <summary>
        /// Logs the user out by clearing the authentication cookie.
        /// </summary>
        /// <returns>An OK result.</returns>
        /// <response code="200">If the user was successfully logged out.</response>
        authGroup.MapPost("/logout", async (HttpContext httpContext) =>
        {
            await httpContext.SignOutAsync();
            return Results.Ok();
        });
    }
}
