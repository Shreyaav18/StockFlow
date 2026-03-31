using FluentValidation;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using StockFlow.Web.BackgroundJobs;
using StockFlow.Web.Common;
using StockFlow.Web.Data;
using StockFlow.Web.Hubs;
using StockFlow.Web.Middleware;
using StockFlow.Web.Services;
using StockFlow.Web.Services.Interfaces;
using StockFlow.Web.Validators;
using System.Threading.RateLimiting;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Hangfire", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] {CorrelationId} {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("logs/stockflow-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {CorrelationId} {Message:lj} {Properties}{NewLine}{Exception}")
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

var disableHttps = string.Equals(
    builder.Configuration["DISABLE_HTTPS_REDIRECT"], "true",
    StringComparison.OrdinalIgnoreCase);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string not configured.");

// ── Step 1: Run EF migrations BEFORE registering Hangfire ──────────────────
// Hangfire's SqlServerStorage.Init() immediately tries to open the DB to set
// up its schema. If the DB doesn't exist yet, it throws "Cannot open database".
// We create + migrate the DB here, before the DI container is built, so
// Hangfire always finds an existing database.
Log.Information("Applying EF migrations...");
var migrationOptions = new DbContextOptionsBuilder<AppDbContext>()
    .UseSqlServer(connectionString)
    .Options;

const int maxMigrationRetries = 10;
for (var attempt = 1; attempt <= maxMigrationRetries; attempt++)
{
    try
    {
        await using var migrationContext = new AppDbContext(migrationOptions);
        await migrationContext.Database.MigrateAsync();
        await DbSeeder.SeedAsync(migrationContext);
        Log.Information("Migrations applied successfully.");
        break;
    }
    catch (Exception ex) when (attempt < maxMigrationRetries)
    {
        Log.Warning("Migration attempt {Attempt}/{Max} failed: {Message}. Retrying in 5s...",
            attempt, maxMigrationRetries, ex.Message);
        await Task.Delay(TimeSpan.FromSeconds(5));
    }
}

// ── Step 2: Register services — DB is guaranteed to exist now ──────────────
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddMemoryCache();

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo("/app/dataprotection-keys"))
    .SetApplicationName("StockFlow");

builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(connectionString, new SqlServerStorageOptions
    {
        CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
        SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
        QueuePollInterval = TimeSpan.Zero,
        UseRecommendedIsolationLevel = true,
        DisableGlobalLocks = true
    }));

builder.Services.AddHangfireServer();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = disableHttps
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.Name = "StockFlow.Auth";
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("ManagerAndAbove", policy => policy.RequireRole("Admin", "Manager"));
    options.AddPolicy("AllStaff", policy => policy.RequireRole("Admin", "Manager", "Staff"));
});

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("LoginPolicy", limiter =>
    {
        limiter.PermitLimit = 5;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiter.QueueLimit = 0;
    });

    options.AddFixedWindowLimiter("GlobalPolicy", limiter =>
    {
        limiter.PermitLimit = 100;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiter.QueueLimit = 10;
    });

    options.OnRejected = async (context, ct) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsync("Too many requests. Please try again later.", ct);
        Log.Warning("Rate limit exceeded for {IP}", context.HttpContext.Connection.RemoteIpAddress);
    };
});

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "StockFlow.Antiforgery";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = disableHttps
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

builder.Services.AddValidatorsFromAssemblyContaining<LoginValidator>();

builder.Services.AddSignalR();
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IItemService, ItemService>();
builder.Services.AddScoped<IShipmentService, ShipmentService>();
builder.Services.AddScoped<IProcessService, ProcessService>();
builder.Services.AddScoped<IWeightValidatorService, WeightValidatorService>();
builder.Services.AddScoped<ITreeBuilderService, TreeBuilderService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<ISearchService, SearchService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IExportService, ExportService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<ShipmentAlertJob>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    if (!disableHttps)
    {
        app.UseHsts();
        app.UseHttpsRedirection();
    }
}
else
{
    app.UseHttpsRedirection();
}

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionMiddleware>();
app.UseStaticFiles();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.UseHangfireDashboard(
    builder.Configuration["Hangfire:DashboardPath"] ?? "/jobs",
    new DashboardOptions
    {
        Authorization = new[] { new HangfireAuthFilter() }
    });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");

app.MapHub<StockFlowHub>("/hubs/stockflow");

var recurringJobs = app.Services.GetRequiredService<IRecurringJobManager>();
recurringJobs.AddOrUpdate<ShipmentAlertJob>(
    "stale-shipment-alert",
    job => job.RunAsync(),
    Cron.Daily(2, 0));

await app.RunAsync();