using System.Security.Claims;
using System.Text;
using personal_timeline_backend.Data;
using personal_timeline_backend.Endpoints;
using personal_timeline_backend.Models;
using personal_timeline_backend.Services;
using personal_timeline_backend.Services.ThirdPartyApis;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Net.Http.Headers;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Define a CORS policy name
var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

// Add services to the container.
builder.Services.AddDbContext<TimelineContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<TokenService>();

// Register HttpClient for API calls
builder.Services.AddHttpClient();

// Add HttpContextAccessor for accessing HttpContext in services
builder.Services.AddHttpContextAccessor();

// Add logging services
builder.Services.AddLogging();

// Register Third-Party API Services
builder.Services.AddScoped<IThirdPartyApiService, GitHubService>();
builder.Services.AddScoped<IThirdPartyApiService, NotionService>();



builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie()
    .AddJwtBearer(options =>
    {
        var jwtIssuer = builder.Configuration["Jwt:Issuer"];
        var jwtAudience = builder.Configuration["Jwt:Audience"];
        var jwtKey = builder.Configuration["Jwt:Key"];

        if (string.IsNullOrEmpty(jwtIssuer) || string.IsNullOrEmpty(jwtAudience) || string.IsNullOrEmpty(jwtKey))
        {
            throw new InvalidOperationException("JWT Issuer, Audience, or Key is not configured in appsettings.json.");
        }

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    })
    .AddGoogle(options =>
    {
        var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
        var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];

        if (string.IsNullOrEmpty(googleClientId) || string.IsNullOrEmpty(googleClientSecret))
        {
            throw new InvalidOperationException("Google ClientId or ClientSecret is not configured in appsettings.json.");
        }

        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret;
        options.CallbackPath = "/auth/google/callback";
        options.Events.OnCreatingTicket = async context =>
        {
            var dbContext = context.HttpContext.RequestServices.GetRequiredService<TimelineContext>();
            var claims = context.Principal!.Claims; // Add !
            var googleId = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.OAuthProvider == "Google" && u.OAuthId == googleId);

            var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value ?? string.Empty;
            var name = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value ?? string.Empty;
            var picture = claims.FirstOrDefault(c => c.Type == "urn:google:picture")?.Value ??
                          claims.FirstOrDefault(c => c.Type == "picture")?.Value ?? string.Empty;

            if (user == null)
            {
                user = new User
                {
                    OAuthProvider = "Google",
                    OAuthId = googleId!, // Add !
                    Email = email,
                    DisplayName = name,
                    ProfileImageUrl = picture,
                    CreatedAt = DateTime.UtcNow,
                    LastLoginAt = DateTime.UtcNow
                };
                dbContext.Users.Add(user);
            }
            else
            {
                user.LastLoginAt = DateTime.UtcNow;
                user.DisplayName = name;
                user.ProfileImageUrl = picture;
            }

            await dbContext.SaveChangesAsync();
            
            // Replace the NameIdentifier claim with our database user ID
            var identity = (ClaimsIdentity)context.Principal!.Identity!; // Add !
            var existingClaim = identity.FindFirst(ClaimTypes.NameIdentifier);
            if(existingClaim != null)
            {
                identity.RemoveClaim(existingClaim);
                identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));
            }
        };
    })
    .AddOAuth("GitHub", options =>
    {
        var githubClientId = builder.Configuration["Authentication:GitHub:ClientId"];
        var githubClientSecret = builder.Configuration["Authentication:GitHub:ClientSecret"];

        if (string.IsNullOrEmpty(githubClientId) || string.IsNullOrEmpty(githubClientSecret))
        {
            throw new InvalidOperationException("GitHub ClientId or ClientSecret is not configured in appsettings.json.");
        }

        options.ClientId = githubClientId;
        options.ClientSecret = githubClientSecret;
        options.CallbackPath = "/auth/github/callback";
        options.AuthorizationEndpoint = "https://github.com/login/oauth/authorize";
        options.TokenEndpoint = "https://github.com/login/oauth/access_token";
        options.UserInformationEndpoint = "https://api.github.com/user";

        options.Scope.Add("read:user");
        options.Scope.Add("repo");

        options.SaveTokens = true;

        options.Events = new OAuthEvents
        {
            OnCreatingTicket = async context =>
            {
                var request = new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.AccessToken!);

                var response = await context.Backchannel.SendAsync(request, context.HttpContext.RequestAborted);
                response.EnsureSuccessStatusCode();

                using (var user = JsonDocument.Parse(await response.Content.ReadAsStringAsync()))
                {
                    var dbContext = context.HttpContext.RequestServices.GetRequiredService<TimelineContext>();
                    
                    // Retrieve the UserId from the AuthenticationProperties
                    if (!context.Properties.Items.TryGetValue("UserId", out var userIdString) || !int.TryParse(userIdString, out var userId))
                    {
                        // This should not happen if RequireAuthorization is used correctly
                        // Log an error or throw an exception if the userId is not found
                        throw new InvalidOperationException("User ID not found in authentication properties during GitHub OAuth callback.");
                    }

                    var existingConnection = await dbContext.ApiConnections
                        .FirstOrDefaultAsync(ac => ac.UserId == userId && ac.ApiProvider == "GitHub");

                    if (existingConnection == null)
                    {
                        dbContext.ApiConnections.Add(new ApiConnection
                        {
                            UserId = userId,
                            ApiProvider = "GitHub",
                            AccessToken = context.AccessToken!,
                            RefreshToken = context.RefreshToken, // GitHub refresh tokens require a separate flow, may be null
                            TokenExpiresAt = context.ExpiresIn.HasValue
                                ? DateTime.UtcNow.AddSeconds(context.ExpiresIn.Value.TotalSeconds)
                                : DateTime.UtcNow.AddYears(100), // Default to 100 years if not provided
                            IsActive = true,
                            Settings = ""
                        });
                    }
                    else
                    {
                        existingConnection.AccessToken = context.AccessToken!;
                        existingConnection.RefreshToken = context.RefreshToken ?? existingConnection.RefreshToken;
                        existingConnection.TokenExpiresAt = context.ExpiresIn.HasValue
                            ? DateTime.UtcNow.AddSeconds(context.ExpiresIn.Value.TotalSeconds)
                            : DateTime.UtcNow.AddYears(100); // Default to 100 years if not provided
                        existingConnection.IsActive = true;
                    }
                    await dbContext.SaveChangesAsync();

                    // Add our internal userId to the ClaimsPrincipal so it's available in the cookie
                    var identity = (ClaimsIdentity)context.Principal!.Identity!;
                    var existingClaim = identity.FindFirst(ClaimTypes.NameIdentifier);
                    if (existingClaim != null)
                    {
                        identity.RemoveClaim(existingClaim);
                    }
                    identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, userId.ToString()));
                }
            }
        };
    })
    .AddOAuth("Notion", options => {
        var notionClientId = builder.Configuration["Authentication:Notion:ClientId"];
        var notionClientSecret = builder.Configuration["Authentication:Notion:ClientSecret"];

        if (string.IsNullOrEmpty(notionClientId) || string.IsNullOrEmpty(notionClientSecret))
        {
            throw new InvalidOperationException("Notion ClientId or ClientSecret is not configured in appsettings.json.");
        }

        options.ClientId = notionClientId;
        options.ClientSecret = notionClientSecret;
        options.CallbackPath = "/auth/notion/callback";
        options.AuthorizationEndpoint = "https://api.notion.com/v1/oauth/authorize";
        options.TokenEndpoint = "https://api.notion.com/v1/oauth/token";
        options.SaveTokens = true;

        // Create a custom HttpClient with Basic Auth for Notion's token endpoint
        var httpClient = new HttpClient();
        var basicAuth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{notionClientId}:{notionClientSecret}"));
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", basicAuth);
        options.Backchannel = httpClient;

        options.Events = new OAuthEvents
        {
            OnCreatingTicket = async context =>
            {
                var dbContext = context.HttpContext.RequestServices.GetRequiredService<TimelineContext>();

                if (!context.Properties.Items.TryGetValue("UserId", out var userIdString) || !int.TryParse(userIdString, out var userId))
                {
                    throw new InvalidOperationException("User ID not found in authentication properties during Notion OAuth callback.");
                }

                var existingConnection = await dbContext.ApiConnections
                    .FirstOrDefaultAsync(ac => ac.UserId == userId && ac.ApiProvider == "Notion");

                if (existingConnection == null)
                {
                    dbContext.ApiConnections.Add(new ApiConnection
                    {
                        UserId = userId,
                        ApiProvider = "Notion",
                        AccessToken = context.AccessToken!,
                        RefreshToken = null, // Notion tokens don't expire
                        TokenExpiresAt = DateTime.UtcNow.AddYears(100),
                        IsActive = true,
                        Settings = ""
                    });
                }
                else
                {
                    existingConnection.AccessToken = context.AccessToken!;
                    existingConnection.IsActive = true;
                }
                await dbContext.SaveChangesAsync();

                var identity = (ClaimsIdentity)context.Principal!.Identity!;
                var existingClaim = identity.FindFirst(ClaimTypes.NameIdentifier);
                if (existingClaim != null)
                {
                    identity.RemoveClaim(existingClaim);
                }
                identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, userId.ToString()));
            }
        };
    })
    ;

builder.Services.AddAuthorization();

// Add CORS services
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
                      builder =>
                      {
                          builder.WithOrigins("https://localhost:5173", "https://97017e158579.ngrok-free.app") // <--- UPDATED TO HTTPS and added ngrok URL
                                 .AllowAnyHeader()
                                 .AllowAnyMethod()
                                 .AllowCredentials(); // Important for cookies
                      });
});


// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
// builder.Services.AddOpenApi();

var app = builder.Build();

// Automatically apply pending migrations on startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<TimelineContext>();
        context.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");
    }
}

// Configure the HTTP request pipeline.
// if (app.Environment.IsDevelopment())
// {
//     app.MapOpenApi();
// }

app.UseHttpsRedirection();

// Use CORS middleware
app.UseCors(MyAllowSpecificOrigins);

app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();
app.MapTimelineEndpoints();
app.MapApiConnectionEndpoints();

app.Run();
