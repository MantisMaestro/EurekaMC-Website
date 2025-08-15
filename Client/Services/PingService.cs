using Client.Services.Data_Service;
using CSnakes.Runtime;

namespace Client.Services;

public class PingService(ILogger<PingService> logger, IServiceScopeFactory serviceScopeFactory)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Ping Service Started.");
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(10));

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

            await Task.Delay(1000, stoppingToken);
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task PingServer()
    {
        var scope = serviceScopeFactory.CreateScope();
        var pythonEnv = scope.ServiceProvider.GetRequiredService<IPythonEnvironment>();
        var module = pythonEnv.PingServer();
        var result = module.Main();

        var dataService = scope.ServiceProvider.GetRequiredService<IDataService>();
        await dataService.UpdateLedger(result.ToArray());
    }
}