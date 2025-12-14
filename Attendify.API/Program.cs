using Attendify.API.Services;
using Attendify.DATA;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add this at the very top of Program.cs
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

// Add services to the container

// 1. Add DbContext (PostgreSQL / Supabase)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Add controllers
builder.Services.AddControllers();

// 3. Add Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();


// 4. Add custom services
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();

// 5. Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
});

// 6. Build the app
var app = builder.Build();

// 7. Configure the HTTP request pipeline



// Add HTTP logging for debugging
app.Use(async (context, next) =>
{
    Console.WriteLine($"API Request: {context.Request.Method} {context.Request.Path}");
    await next();
    Console.WriteLine($"API Response: {context.Response.StatusCode}");
});

// Use CORS policy
app.UseCors("AllowAll");

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Add a simple test endpoint for connectivity testing
app.MapGet("/api/test", () => new { message = "API is running", timestamp = DateTime.UtcNow });

// Add a health check endpoint
app.MapGet("/health", () => new { status = "OK", time = DateTime.UtcNow });

// Start the app
try
{
    Console.WriteLine($"API starting on: {app.Urls}");
    app.Run();
}
catch (Exception ex)
{
    Console.WriteLine($"API failed to start: {ex.Message}");
    throw;
}