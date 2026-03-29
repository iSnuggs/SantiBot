using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using SantiBot.Dashboard.Hubs;
using SantiBot.Dashboard.Middleware;
using SantiBot.Dashboard.Services;

var builder = WebApplication.CreateBuilder(args);

// JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? "SantiBot-Dashboard-Secret-Key-Change-In-Production-Min32Chars!";
var keyBytes = Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "SantiBot.Dashboard",
            ValidAudience = "SantiBot.Dashboard",
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
        };

        // SignalR sends JWT via query string instead of Authorization header
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                    context.Token = accessToken;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddHttpClient();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "SantiBot Dashboard API",
        Version = "v1",
        Description = "API for managing SantiBot — a Discord bot with 1000+ features",
    });
});
// SignalR for real-time dashboard updates between users
builder.Services.AddSignalR();

// Register dashboard services
builder.Services.AddSingleton<DiscordOAuthService>();
builder.Services.AddSingleton<JwtService>();
// Stores Discord tokens in memory so we can fetch guilds on behalf of users
builder.Services.AddSingleton<TokenStorageService>();
// Checks and caches whether users can manage specific guilds
builder.Services.AddSingleton<GuildPermissionService>();

// CORS for the frontend
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
                builder.Configuration["Dashboard:FrontendUrl"] ?? "http://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

app.UseCors();
app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "SantiBot API v1"));
app.UseAuthentication();
app.UseAuthorization();
// Verify the user has "Manage Server" permission before allowing guild config access
app.UseMiddleware<GuildPermissionMiddleware>();
app.MapControllers();
// SignalR hub endpoint — frontend connects here for real-time updates
app.MapHub<DashboardHub>("/hubs/dashboard");

// Health check
app.MapGet("/api/health", () => Results.Ok(new { status = "healthy", bot = "SantiBot" }));

app.Run();
