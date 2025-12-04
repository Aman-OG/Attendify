


using Attendify.DATA;
using Microsoft.EntityFrameworkCore;
using DotNetEnv;



var supabaseUrl = Environment.GetEnvironmentVariable("SUPABASE_URL");
var supabaseKey = Environment.GetEnvironmentVariable("SUPABASE_KEY");




var builder = WebApplication.CreateBuilder(args);

// Load .env
DotNetEnv.Env.Load();
var connectionString = Environment.GetEnvironmentVariable("SUPABASE_KEY"); 
var dbUrl = Environment.GetEnvironmentVariable("SUPABASE_URL");
var fullConnectionString = "Host=YOUR_HOST;Port=5432;Database=YOUR_DB;Username=YOUR_USER;Password=YOUR_PASS";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(fullConnectionString));

builder.Services.AddControllers();


builder.Services.AddOpenApi();
var app = builder.Build();






// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
