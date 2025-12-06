using Attendify.DATA;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Models;         
var builder = WebApplication.CreateBuilder(args);

// ---- REMOVE THIS LINE COMPLETELY (it was breaking the connection) ----
// AppContext.SetSwitch("Npgsql.EnableIPv6", false);

// Add DbContext (PostgreSQL / Supabase)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add controllers
builder.Services.AddControllers();

// Add Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();


app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();