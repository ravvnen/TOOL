using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NATS.Client.Core;
using NATS.Client.JetStream;
using TOOL.Infrastructure.Database;
using TOOL.Infrastructure.Messaging;
using TOOL.Modules.DeltaProjection;
using TOOL.Modules.MemoryManagement;
using TOOL.Modules.Promotion;
using TOOL.Modules.Replay;
using TOOL.Modules.SeedProcessing;
using TOOL.Modules.SeedProcessing.Ingestion;

namespace TOOL;

/// <summary>
/// Service registration extension. Keeps Program.cs minimal and avoids extra top-level statements.
/// </summary>
public static class ServiceConfiguration
{
    public static void AddApplicationServices(this WebApplicationBuilder builder)
    {
        // Controllers
        builder
            .Services.AddControllers()
            .AddJsonOptions(o => o.JsonSerializerOptions.PropertyNameCaseInsensitive = true);

        // CORS for Agent.Container communication
        builder.Services.AddCors(options =>
            options.AddDefaultPolicy(policy =>
                policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()
            )
        );

        // NATS connection + JetStream
        var natsUrl = Environment.GetEnvironmentVariable("NATS_URL") ?? "nats://localhost:4222";
        builder.Services.AddSingleton<NatsConnection>(_ => new NatsConnection(
            new NatsOpts { Url = natsUrl }
        ));
        builder.Services.AddSingleton(sp => new NatsJSContext(
            sp.GetRequiredService<NatsConnection>()
        ));

        // HTTP client for external calls
        builder.Services.AddHttpClient<DataSeeder>();

        // Domain services
        builder.Services.AddSingleton<ISeedHandler, SeedHandler>();
        builder.Services.AddSingleton<DataSeeder>();

        // Centralized database and memory services
        builder.Services.AddSingleton<AppSqliteFactory>();
        builder.Services.AddSingleton<MemoryCompiler>();
        builder.Services.AddSingleton<ReplayEngine>();

        // Stream bootstrappers
        builder.Services.AddSingleton<IHostedService>(sp => new StreamBootstrapperService(
            sp.GetRequiredService<NatsJSContext>(),
            "EVENTS",
            new[] { "evt.>" }
        ));
        builder.Services.AddSingleton<IHostedService>(sp => new StreamBootstrapperService(
            sp.GetRequiredService<NatsJSContext>(),
            "DELTAS",
            new[] { "delta.>" }
        ));
        builder.Services.AddSingleton<IHostedService>(sp => new StreamBootstrapperService(
            sp.GetRequiredService<NatsJSContext>(),
            "AUDITS",
            new[] { "audit.>" }
        ));

        // Promoter (optional)
        var promoterEnabled = (
            Environment.GetEnvironmentVariable("PROMOTER_ENABLED") ?? "true"
        ).Equals("true", StringComparison.OrdinalIgnoreCase);
        if (promoterEnabled)
        {
            builder.Services.AddSingleton<IHostedService>(sp => new Promoter(
                sp.GetRequiredService<NatsJSContext>(),
                sp.GetRequiredService<ILogger<Promoter>>()
            ));
        }

        // Database initialization - ensure database exists on startup
        builder.Services.AddSingleton<IHostedService>(sp =>
        {
            var dbFactory = sp.GetRequiredService<AppSqliteFactory>();
            var logger = sp.GetRequiredService<ILogger<DatabaseInitializer>>();
            return new DatabaseInitializer(dbFactory, logger);
        });

        // Delta stream consumer for centralized processing
        builder.Services.AddSingleton<IHostedService, DeltaStreamConsumerService>();
    }
}
