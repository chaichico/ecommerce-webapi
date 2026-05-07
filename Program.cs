using Microsoft.EntityFrameworkCore;
using Data;
using Services.Interfaces;
using Repositories.Interfaces;
using Repositories;
using Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.OpenApi.Models;
using Serilog;
using Logging;
using Middleware;

// Serilog setup
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .Enrich.WithThreadId()
    .WriteTo.Async(a => a.File(
        path: "logs/audit-.json",
        formatter: new Serilog.Formatting.Json.JsonFormatter(),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30))
    .CreateLogger();

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog();

string jwtKey = builder.Configuration["Jwt:Key"]!;

// DbContext - Use environment variable for server or fallback to config
string? connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        connectionString,
        sqlServerOptions =>
        {
            sqlServerOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorNumbersToAdd: null);
        }));

// Controller
builder.Services.AddControllers();
// Register UnitOfWork and DbContext transaction support
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
// Register services
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IEncryptionService, EncryptionService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();
// Order service
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderService, OrderService>();
// Auto Mapper
builder.Services.AddAutoMapper(cfg => { }, typeof(Mappings.OrderProfile).Assembly);

// Register Logging Services
builder.Services.AddSingleton<ILogChannel, LogChannel>();
builder.Services.AddSingleton<LogSummaryService>();
builder.Services.AddHostedService<LogBackgroundService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<LogSummaryService>());

// ← เพิ่ม JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey))
        };
    });


// Swagger
builder.Services.AddEndpointsApiExplorer();
// using Microsoft.OpenApi.Models;

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "ใส่ JWT Token"
    });

    options.AddSecurityDefinition("Basic", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "basic",
        In = ParameterLocation.Header,
        Description = "ใส่ username และ password สำหรับ Admin"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new List<string>()
        }
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Basic"
                }
            },
            new List<string>()
        }
    });
});

builder.Services.AddAuthorization();
// Build app
WebApplication app = builder.Build();

// Seed Database
using (IServiceScope scope = app.Services.CreateScope())
{
    IServiceProvider services = scope.ServiceProvider;
    AppDbContext context = services.GetRequiredService<AppDbContext>();
    IPasswordHasher passwordHasher = services.GetRequiredService<IPasswordHasher>();
    IEncryptionService encryptionService = services.GetRequiredService<IEncryptionService>();
    await DbSeeder.SeedAsync(context, passwordHasher, encryptionService);
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Ecommerce API v1");
    c.EnablePersistAuthorization();
});
app.UseHttpsRedirection();

// Logging Middleware - must be before Authentication
app.UseMiddleware<LoggingMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();
