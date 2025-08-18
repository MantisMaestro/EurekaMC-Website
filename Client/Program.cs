using Client.Components;
using Client.Services;
using Client.Services.Data_Service;
using CSnakes.Runtime;
using CSnakes.Runtime.Locators;
using EurekaDb.Context;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();

builder.Services
    .WithPython()
    .WithHome(Environment.CurrentDirectory)
    .WithVirtualEnvironment(Path.Combine(Environment.CurrentDirectory, ".venv"))
    .FromRedistributable(RedistributablePythonVersion.Python3_12)
    .WithPipInstaller();

builder.Services.AddScoped<IDataService, DataService>();

builder.Services.AddHostedService<PingService>();

builder.Services.AddDbContext<EurekaContext>(e => e.UseSqlite(
    builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
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

app.Run();