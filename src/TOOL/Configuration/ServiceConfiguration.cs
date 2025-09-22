using NATS.Client.Core;
using NATS.Client.JetStream;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
namespace TOOL;

/// <summary>
/// Service registration extension. Keeps Program.cs minimal and avoids extra top-level statements.
/// </summary>
public static class ServiceConfiguration
{
    public static void AddApplicationServices(this WebApplicationBuilder builder)
    {
        // Controllers
        builder.Services.AddControllers()
            .AddJsonOptions(o => o.JsonSerializerOptions.PropertyNameCaseInsensitive = true);

        // NATS connection + JetStream
        var natsUrl = Environment.GetEnvironmentVariable("NATS_URL") ?? "nats://localhost:4222";
        builder.Services.AddSingleton<NatsConnection>(_ => new NatsConnection(new NatsOpts { Url = natsUrl }));
        builder.Services.AddSingleton(sp => new NatsJSContext(sp.GetRequiredService<NatsConnection>()));

        // HTTP client for external calls
        builder.Services.AddHttpClient<DataSeeder>();

        // Domain services
        builder.Services.AddSingleton<IWebhookService, WebhookService>();
        builder.Services.AddSingleton<DataSeeder>();

        // Stream bootstrappers
        builder.Services.AddSingleton<IHostedService>(sp => new StreamBootstrapper(
            sp.GetRequiredService<NatsJSContext>(), "EVENTS", new[] { "evt.>" }));
        builder.Services.AddSingleton<IHostedService>(sp => new StreamBootstrapper(
            sp.GetRequiredService<NatsJSContext>(), "DELTAS", new[] { "delta.>" }));
    }
}