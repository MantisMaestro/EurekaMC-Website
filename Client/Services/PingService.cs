using Client.Services.Data_Service;
using Python.Runtime;

namespace Client.Services;

public class PingService(ILogger<PingService> logger, IConfiguration configuration, IServiceScopeFactory scopeFactory)
    : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Ping Service Started.");
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));

        Runtime.PythonDLL = configuration["PythonRuntime"];

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

            await Task.Delay(1000, cancellationToken);
        } while (await timer.WaitForNextTickAsync(cancellationToken));
    }

    private async Task PingServer()
    {
        using (Py.GIL())
        {
            dynamic module = Py.Import("ping_server");
            var result = module.ping_server("173.240.152.72", 9000);
            var response = (string[])result;

            var scope = scopeFactory.CreateScope();
            var dataService = scope.ServiceProvider.GetRequiredService<IDataService>();
            await dataService.UpdateLedger(response);
        }

        await Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Ping Service stopped at: {time}", DateTimeOffset.Now);
        await Task.CompletedTask;
    }
}