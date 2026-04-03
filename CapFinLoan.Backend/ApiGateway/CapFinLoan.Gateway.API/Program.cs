using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Explicitly configure URL to ensure fixed port
builder.WebHost.UseUrls("http://localhost:7000");

// Add detailed logging for debugging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Debug);

builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

var jwtSection = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSection["Key"] ?? throw new InvalidOperationException("JWT key is missing.");
var issuer = jwtSection["Issuer"] ?? throw new InvalidOperationException("JWT issuer is missing.");
var audience = jwtSection["Audience"] ?? throw new InvalidOperationException("JWT audience is missing.");
var key = Encoding.UTF8.GetBytes(secretKey);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ClockSkew = TimeSpan.FromMinutes(1),
            RoleClaimType = System.Security.Claims.ClaimTypes.Role,
            NameClaimType = System.Security.Claims.ClaimTypes.NameIdentifier
        };
    });

builder.Services.AddOcelot();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:5174")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

Console.WriteLine("========================================");
Console.WriteLine("API Gateway Started");
Console.WriteLine($"Listening on: http://localhost:7000");
Console.WriteLine($"Environment: {app.Environment.EnvironmentName}");
Console.WriteLine("Routes configured:");
Console.WriteLine("  /api/auth/* -> http://localhost:7001");
Console.WriteLine("  /api/applications/* -> http://localhost:7002");
Console.WriteLine("  /api/documents/* -> http://localhost:7003");
Console.WriteLine("  /api/admin/* -> http://localhost:7004");
Console.WriteLine("========================================");

await app.UseOcelot();

app.Run();
