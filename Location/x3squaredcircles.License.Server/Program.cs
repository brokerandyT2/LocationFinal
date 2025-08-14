using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using x3squaredcircles.License.Server.Data;
using x3squaredcircles.License.Server.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add Entity Framework with SQLite
builder.Services.AddDbContext<LicenseDbContext>(options =>
    options.UseSqlite("Data Source=/data/license.db"));

// Add custom services
builder.Services.AddScoped<ILicenseService, LicenseService>();
builder.Services.AddSingleton<ILicenseConfigService, LicenseConfigService>();
builder.Services.AddSingleton<IEncryptionValidationService, EncryptionValidationService>();
builder.Services.AddScoped<IDatabaseMigrationService, DatabaseMigrationService>();

// Add logging
builder.Logging.AddConsole();

var app = builder.Build();

// Validate encryption on startup
using (var scope = app.Services.CreateScope())
{
    var encryptionValidator = scope.ServiceProvider.GetRequiredService<IEncryptionValidationService>();
    var isEncrypted = await encryptionValidator.ValidateDataMountEncryptionAsync();

    if (!isEncrypted)
    {
        app.Logger.LogCritical("SECURITY ERROR: Data mount /data is not encrypted - container will not start");
        Environment.Exit(1);
    }

    app.Logger.LogInformation("✓ Data mount encryption validated");
}

// Initialize database and configuration
using (var scope = app.Services.CreateScope())
{
    var migrationService = scope.ServiceProvider.GetRequiredService<IDatabaseMigrationService>();
    var configService = scope.ServiceProvider.GetRequiredService<ILicenseConfigService>();

    // Run database migrations
    await migrationService.MigrateAsync();

    // Initialize license configuration from embedded config
    await configService.InitializeLicenseConfigAsync();

    app.Logger.LogInformation("✓ Database and license configuration initialized");
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    timestamp = DateTime.UtcNow,
    version = "1.0.0"
}));

// Metrics endpoint for monitoring
app.MapGet("/metrics", async (ILicenseService licenseService) =>
{
    var status = await licenseService.GetLicenseStatusAsync();
    return Results.Ok(new
    {
        concurrent_sessions = status.CurrentConcurrent,
        max_concurrent = status.MaxConcurrent,
        burst_events_used = status.MonthlyBurstsUsed,
        burst_events_remaining = status.MonthlyBurstsRemaining,
        licensed_tools_count = status.LicensedTools.Count,
        uptime_seconds = Environment.TickCount64 / 1000
    });
});

app.Logger.LogInformation("🔒 Licensing Container started successfully");
app.Logger.LogInformation("📊 Health check: GET /health");
app.Logger.LogInformation("📈 Metrics: GET /metrics");
app.Logger.LogInformation("🔑 License API: /license/*");

app.Run();