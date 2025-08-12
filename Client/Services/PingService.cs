using Python.Runtime;

namespace Client.Services;

public class PingService(ILogger<PingService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Ping Service Started.");
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));


        while (await timer.WaitForNextTickAsync(cancellationToken))
        {
            try
            {
                await PingServer();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ping Service Error.");
            }

            await Task.Delay(1000, cancellationToken);
        }
    }

    private async Task PingServer()
    {
        using (Py.GIL())
        {
            
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Ping Service stopped at: {time}", DateTimeOffset.Now);
        await Task.CompletedTask;
    }
}