using Microsoft.EntityFrameworkCore;
using Data;
using Controllers;

var builder = WebApplication.CreateBuilder(args);

// DbContext - Use environment variable for server or fallback to config
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var dbServer = builder.Configuration["DB_SERVER"];
if (!string.IsNullOrEmpty(dbServer))
{
    connectionString = $"Server={dbServer},{builder.Configuration["DB_PORT"] ?? "1433"};Database={builder.Configuration["DB_NAME"] ?? "EcommerceDb"};User={builder.Configuration["DB_USER"] ?? "sa"};Password={builder.Configuration["DB_PASSWORD"]};TrustServerCertificate=True;";
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString)
);

// Controller
builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Seed Database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<AppDbContext>();
    await DbSeeder.SeedAsync(context);
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();