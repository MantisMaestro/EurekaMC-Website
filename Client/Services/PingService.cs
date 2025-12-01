using Client.Services.Data_Service;
using MCStatus;

namespace Client.Services;

public class PingService(
    ILogger<PingService> logger,
    IConfiguration configuration,
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

        var address = configuration["Server:IP"];
        var port = configuration["Server:Port"];
        if (string.IsNullOrEmpty(address) || string.IsNullOrEmpty(port)) return;
        
        var status = await ServerListClient.GetStatusAsync(address, Convert.ToUInt16(port));

        var dataService = scope.ServiceProvider.GetRequiredService<IDataService>();
        await dataService.UpdateLedger(status.Players.Sample);
    }
}