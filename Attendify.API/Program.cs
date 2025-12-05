using Attendify.DATA;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
AppContext.SetSwitch("Npgsql.EnableIPv6", false);

var builder = WebApplication.CreateBuilder(args);

// Read connection string
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));
Console.WriteLine(builder.Configuration.GetConnectionString("DefaultConnection"));

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Attendify API",
        Version = "v1"
    });
});

builder.Services.AddControllers();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Attendify API v1");
    });
}

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();

app.Run();
