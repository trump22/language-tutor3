using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using languagetutor.Data;
using languagetutor.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. DATABASE
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. JWT AUTHENTICATION
var jwtSecret = builder.Configuration["Jwt:Key"] ?? builder.Configuration["Jwt:Secret"];
if (string.IsNullOrWhiteSpace(jwtSecret))
    throw new InvalidOperationException("JWT key is not configured. Set Jwt:Key or Jwt:Secret.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtSecret)),
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// 3. CORS - cho phép frontend local va domain production cau hinh qua Cors:AllowedOrigins
var configuredOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? Array.Empty<string>();

var allowedOrigins = configuredOrigins.Length > 0
    ? configuredOrigins
    : new[]
    {
        "http://localhost:5173",
        "http://127.0.0.1:5173",
        "http://localhost:5174",
        "http://127.0.0.1:5174"
    };

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials());
});

// 4. CONTROLLERS & JSON
builder.Services.AddControllers()
    .AddJsonOptions(opt =>
    {
        opt.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        opt.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        opt.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// 5. HTTP CLIENT (cho GeminiService)
builder.Services.AddHttpClient();

// 6. SERVICES
builder.Services.AddScoped<GeminiService>();
builder.Services.AddScoped<AzureSpeechService>();

// 7. SWAGGER với JWT
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Language Tutor API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Dán trực tiếp mã Token vào đây (không cần gõ chữ Bearer ở trước)."
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await DefaultAiSeedData.SeedAsync(db);
}
catch (Exception ex) when (!app.Environment.IsDevelopment())
{
    app.Logger.LogError(
        ex,
        "Database initialization failed. The web application will start, but database-backed APIs will be unavailable. " +
        "Configure ConnectionStrings__DefaultConnection with a PostgreSQL host reachable from Azure App Service.");
}

var azureHomePath = Environment.GetEnvironmentVariable("HOME");
var uploadsPath = string.IsNullOrWhiteSpace(azureHomePath)
    ? Path.Combine(app.Environment.ContentRootPath, "uploads")
    : Path.Combine(azureHomePath, "data", "uploads");
Directory.CreateDirectory(uploadsPath);

// 8. SERVE REACT BUILD AND AUDIO UPLOADS.
app.Use(async (context, next) =>
{
    context.Response.OnStarting(() =>
    {
        if (context.Response.ContentType?.StartsWith("text/html", StringComparison.OrdinalIgnoreCase) == true)
        {
            context.Response.Headers.CacheControl = "no-cache, no-store, must-revalidate";
            context.Response.Headers.Pragma = "no-cache";
            context.Response.Headers.Expires = "0";
        }

        return Task.CompletedTask;
    });

    await next();
});

app.UseDefaultFiles();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        if (ctx.Context.Request.Path.StartsWithSegments("/assets"))
        {
            ctx.Context.Response.Headers.CacheControl = "public,max-age=31536000,immutable";
        }
    }
});

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads",
    OnPrepareResponse = ctx =>
    {
        var fileName = ctx.File.Name;
        if (fileName.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase) ||
            fileName.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
        {
            ctx.Context.Response.Headers.ContentDisposition = "inline";
        }
    }
});

// 9. SWAGGER UI
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Language Tutor API v1");
    options.RoutePrefix = "swagger";
});
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/api/health", () => Results.Ok(new
{
    status = "ok",
    environment = app.Environment.EnvironmentName,
    timestamp = DateTimeOffset.UtcNow
})).AllowAnonymous();

app.MapGet("/api/health/database", async (AppDbContext db, CancellationToken cancellationToken) =>
{
    try
    {
        var connected = await db.Database.CanConnectAsync(cancellationToken);
        return connected
            ? Results.Ok(new { status = "ok", database = "connected" })
            : Results.Json(
                new { status = "unavailable", database = "disconnected" },
                statusCode: StatusCodes.Status503ServiceUnavailable);
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "Database health check failed.");
        return Results.Json(
            new
            {
                status = "unavailable",
                database = "disconnected",
                message = "Configure ConnectionStrings__DefaultConnection with an Azure-reachable PostgreSQL server."
            },
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
}).AllowAnonymous();

app.MapGet("/api/health/speech", async (
    AzureSpeechService speech,
    CancellationToken cancellationToken) =>
{
    var health = await speech.CheckHealthAsync(cancellationToken);
    return health.Success
        ? Results.Ok(new
        {
            status = "ok",
            service = "azure-speech",
            configured = true,
            reachable = true,
            region = speech.Region,
            providerStatus = health.ProviderStatus
        })
        : Results.Json(
            new
            {
                status = "unavailable",
                service = "azure-speech",
                configured = speech.IsConfigured,
                reachable = false,
                region = speech.Region,
                providerStatus = health.ProviderStatus,
                message = health.Message
            },
            statusCode: StatusCodes.Status503ServiceUnavailable);
}).AllowAnonymous();

if (!app.Environment.IsDevelopment())
{
    app.MapFallbackToFile("index.html");
}

app.Run();
