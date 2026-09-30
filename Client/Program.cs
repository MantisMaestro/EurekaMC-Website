using Client.Components;
using Client.Models;
using Client.Services;
using Client.Services.Data_Service;
using EurekaDb.Context;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();
builder.Services.Configure<ServerSettings>(builder.Configuration.GetSection("Server"));
builder.Services.AddSingleton<IReadOnlyList<MapInfo>>(
    builder.Configuration.GetSection("Maps").Get<List<MapInfo>>() ?? []);
builder.Services.AddScoped<IDataService, DataService>();
builder.Services.AddHostedService<PingService>();
builder.Services.AddDbContext<EurekaContext>(e => e.UseSqlite(
    builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    var db = services.GetRequiredService<EurekaContext>();

    try
    {
        var pendingMigrations = await db.Database.GetPendingMigrationsAsync();
        var migrations = pendingMigrations.ToList();
        if (migrations.Any())
        {
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            if (connectionString?.Contains("Data Source=") == true)
            {
                var dbPath = connectionString.Replace("Data Source=", "").Trim();
                if (File.Exists(dbPath))
                {
                    var backupPath = $"{dbPath}.{DateTime.UtcNow:yyyyMMddHHmmss}.bak";
                    File.Copy(dbPath, backupPath);
                    logger.LogInformation("Database backup created at: {Path}", backupPath);
                }
            }

            logger.LogInformation("Applying {Count} database migration(s)...", migrations.Count);
            await db.Database.MigrateAsync();
            logger.LogInformation("Database migrations applied successfully.");
        }
    }
    catch (Exception ex)
    {
        logger.LogCritical(ex, "An error occurred while migrating the database. The app will not start to prevent data corruption.");
        throw;
    }
}

// app.UseHttpsRedirection();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.MapStaticAssets();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapReverseProxy();

app.Run();