using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using dotnetprojekt.Context;
using dotnetprojekt.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using dotnetprojekt.JwtOptionsSetup;
using dotnetprojekt.Authentication;
using dotnetprojekt.Services;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

//var connectionString = DotNetEnv.Env.GetString("DATABASE_URL");

var builder = WebApplication.CreateBuilder(args);

// Configure logging with more detailed settings
builder.Logging.ClearProviders();
builder.Logging.AddConsole(options => 
{
    options.IncludeScopes = true;
});
builder.Logging.AddDebug();
// Set minimum log level to capture all authentication events
builder.Logging.SetMinimumLevel(LogLevel.Information);

// Enable specific authentication logging categories
builder.Logging.AddFilter("Microsoft.AspNetCore.Authentication", LogLevel.Debug);
builder.Logging.AddFilter("dotnetprojekt.JwtOptionsSetup", LogLevel.Debug);
builder.Logging.AddFilter("dotnetprojekt.Authentication", LogLevel.Debug);

var connectionString = builder.Configuration.GetConnectionString("DATABASE_URL");
// Add services to the container.
builder.Services.AddControllersWithViews();

// Add DbContext
builder.Services.AddDbContext<WineLoversContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
    
    // Add this if you want to see the SQL queries in development
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.LogTo(Console.WriteLine, LogLevel.Information);
    }
});

// Register services
builder.Services.AddScoped<SearchService>();
builder.Services.AddScoped<BlobStorageService>();
builder.Services.AddHostedService<DbInitializer>();
builder.Services.AddScoped<IExperienceLevelService, ExperienceLevelService>();

// Register DataSeeder
//builder.Services.AddDataSeeder();

// JWT TOKEN CONFIGURATION
// 1. Register JWT services
builder.Services.AddScoped<JwtProvider>();

//Used in JwtProvider, dont delete
builder.Services.ConfigureOptions<JwtOptionsSetup>();

// 2. Configure JWT authentication with a SINGLE configuration approach
builder.Services.AddAuthentication(options => 
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options => 
{
    // Basic validation parameters
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(
            builder.Configuration["Jwt:SecretKey"] ?? throw new InvalidOperationException("JWT Secret Key is not configured"))),
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(2)
    };

    // Enable saving the token for later retrieval
    options.SaveToken = true;
    
    // Configure events for cookie handling and proper diagnostics
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            // Extract JWT from cookie
            context.Token = context.Request.Cookies["jwt"];

            // Postman - header jwt
            if (string.IsNullOrEmpty(context.Token))
            {
                var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
                if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
                {
                    context.Token = authHeader.Substring("Bearer ".Length).Trim();
                }
            }

            // Log token status for debugging
            Console.WriteLine($"JWT Cookie Present: {context.Token != null}");
            Console.WriteLine($"Cookie Count: {context.Request.Cookies.Count}");
            
            return Task.CompletedTask;
        },
        
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine($"Authentication Failed: {context.Exception.GetType().Name}: {context.Exception.Message}");
            
            if (context.Exception is SecurityTokenExpiredException)
            {
                context.Response.Headers.Add("Token-Expired", "true");
            }
            
            return Task.CompletedTask;
        },
        
        OnChallenge = context =>
        {
            Console.WriteLine($"Challenge triggered for path: {context.Request.Path}");
            
            // For browser requests, redirect to login page
            if (!context.Request.Path.StartsWithSegments("/api") && 
                context.Request.Headers.Accept.ToString().Contains("text/html"))
            {
                context.Response.Redirect("/Auth/Login");
                context.HandleResponse(); // Prevents default 401 response
            }
            
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

//dodane - autoryzacja i autentykacja
app.UseAuthentication();

app.UseAuthorization();


app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
    
);

// Seed data if in development environment
// if (app.Environment.IsDevelopment())
// {
//     await app.Services.SeedDataAsync();
// }

app.Run();