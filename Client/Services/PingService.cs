using Client.Models;
using Client.Services.Data_Service;
using MCStatus;
using Microsoft.Extensions.Options;

namespace Client.Services;

public class PingService(
    ILogger<PingService> logger,
    IOptions<ServerSettings> serverSettings,
    IServiceScopeFactory serviceScopeFactory)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Ping Service Started.");
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(60));

        do
        {
            try
            {
                await PingServer();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ping Service Error.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task PingServer()
    {
        using var scope = serviceScopeFactory.CreateScope();

        var settings = serverSettings.Value;
        if (string.IsNullOrEmpty(settings.IP) || !ushort.TryParse(settings.Port, out var port)) return;

        var status = await ServerListClient.GetStatusAsync(settings.IP, port);

        var dataService = scope.ServiceProvider.GetRequiredService<IDataService>();
        await dataService.UpdateLedger(status.Players.Sample, 60);
    }
}