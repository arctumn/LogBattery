using Arctumn.LogBattery.Extensions;
using Arctumn.LogBattery.Sample;
using Arctumn.LogBattery.Sample.Auth;

var builder = WebApplication.CreateBuilder(args);

builder.AddCompactLogging("LogBattery.Sample.Api");

builder.Services.AddHostedService<LogSimulator>();

builder.Services
    .AddAuthentication(BasicAuthenticationHandler.SchemeName)
    .AddScheme<BasicAuthenticationOptions, BasicAuthenticationHandler>(
        BasicAuthenticationHandler.SchemeName,
        opts =>
        {
            opts.Username = builder.Configuration["LogViewerUser:Username"] ?? "admin";
            opts.Password = builder.Configuration["LogViewerUser:Password"] ?? "logbattery";
            opts.Realm = "LogBattery";
        });

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseCompactRequestLogging();
app.UseAuthentication();
app.UseAuthorization();

app.RequireLogBatteryAuth(BasicAuthenticationHandler.SchemeName)
   .MapLogViewer();

app.MapGet("/", () => Results.Redirect("/logs"));

app.MapGet("/api/orders", (ILogger<Program> logger) =>
{
    logger.LogInformation("Listing orders");
    var orders = Enumerable.Range(1, 5).Select(i => new
    {
        Id = i,
        Customer = $"Customer {i}",
        Total = Math.Round(i * 19.99m, 2)
    });
    return Results.Ok(orders);
});

app.MapGet("/api/orders/{id:int}", (int id, ILogger<Program> logger) =>
{
    logger.LogInformation("Fetching order {OrderId}", id);

    if (id is < 1 or > 100)
    {
        logger.LogWarning("Order {OrderId} out of range", id);
        return Results.NotFound();
    }

    return Results.Ok(new { Id = id, Customer = $"Customer {id}", Total = Math.Round(id * 19.99m, 2) });
});

app.MapPost("/api/orders", (CreateOrderRequest request, ILogger<Program> logger) =>
{
    var newId = Random.Shared.Next(1000, 9999);
    var safeCustomerForLog = (request.Customer ?? string.Empty).Replace("\r", "").Replace("\n", "");
    logger.LogInformation("Creating order for {Customer} (qty={Quantity})", safeCustomerForLog, request.Quantity);

    if (request.Quantity > 50)
        logger.LogWarning("Large order detected: {Quantity} units for {Customer}", request.Quantity, safeCustomerForLog);

    return Results.Created($"/api/orders/{newId}", new { Id = newId, request.Customer, request.Quantity });
});

app.MapGet("/api/error", (ILogger<Program> logger) =>
{
    logger.LogInformation("About to fail");
    throw new InvalidOperationException("Simulated failure for log demo");
});

app.MapGet("/api/slow", async (int? ms, ILogger<Program> logger) =>
{
    var delay = Math.Clamp(ms ?? 750, 0, 10_000);
    logger.LogInformation("Starting slow operation ({Delay}ms)", delay);
    await Task.Delay(delay);
    logger.LogInformation("Slow operation done");
    return Results.Ok(new { delayMs = delay });
});

app.MapGet("/api/burst", (int? count, ILogger<Program> logger) =>
{
    var n = Math.Clamp(count ?? 50, 1, 1000);
    for (var i = 0; i < n; i++)
    {
        var roll = Random.Shared.Next(100);
        if (roll < 70) logger.LogInformation("Burst event {Index}", i);
        else if (roll < 90) logger.LogWarning("Burst warning {Index}", i);
        else logger.LogError("Burst error {Index}", i);
    }
    return Results.Ok(new { emitted = n });
});

app.Run();

internal sealed record CreateOrderRequest(string Customer, int Quantity);
