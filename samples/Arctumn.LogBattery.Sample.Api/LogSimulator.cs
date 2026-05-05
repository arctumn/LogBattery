namespace Arctumn.LogBattery.Sample;

internal sealed class LogSimulator(ILogger<LogSimulator> logger) : BackgroundService
{
    private static readonly string[] Regions = ["eu-west", "us-east", "ap-south", "sa-east"];
    private static readonly string[] Operations = ["payment.process", "user.signin", "inventory.sync", "report.generate", "email.send"];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try { await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken); }
        catch (OperationCanceledException) { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            EmitBurst();

            try { await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }

    private void EmitBurst()
    {
        var count = Random.Shared.Next(3, 9);
        for (var i = 0; i < count; i++)
        {
            var op = Operations[Random.Shared.Next(Operations.Length)];
            var region = Regions[Random.Shared.Next(Regions.Length)];
            var userId = Random.Shared.Next(1000, 9999);
            var elapsed = Random.Shared.Next(5, 1500);

            var roll = Random.Shared.Next(100);
            if (roll < 70)
                logger.LogInformation("{Operation} completed in {Elapsed}ms (User={UserId}, Region={Region})", op, elapsed, userId, region);
            else if (roll < 90)
                logger.LogWarning("{Operation} slow ({Elapsed}ms) (User={UserId}, Region={Region})", op, elapsed, userId, region);
            else
                logger.LogError("{Operation} failed (User={UserId}, Region={Region})", op, userId, region);
        }
    }
}
