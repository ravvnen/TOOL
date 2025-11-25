using NATS.Client.Core;
using NATS.Client.JetStream;
using Infrastructure.Database;
using Infrastructure.Database.Schema;
using Infrastructure.Messaging;
using Modules.DeltaProjection;
using Modules.MemoryManagement;
using Modules.Promotion;
using Modules.Replay;
using Modules.SeedProcessing;

namespace Configuration;

/// <summary>
/// Service registration extension. Keeps Program.cs minimal and avoids extra top-level statements.
/// </summary>
public static class ServiceConfiguration
{
    private static readonly string[] EventStreamSubjects = new[] { "evt.>" };
    private static readonly string[] DeltaStreamSubjects = new[] { "delta.>" };
    private static readonly string[] AuditStreamSubjects = new[] { "audit.>" };

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
        builder.Services.AddSingleton<RulesDbSchema>();
        builder.Services.AddSingleton<MemoryCompiler>();
        builder.Services.AddSingleton<ReplayEngine>();

        // Stream bootstrappers
        builder.Services.AddSingleton<IHostedService>(sp => new StreamBootstrapperService(
            sp.GetRequiredService<NatsJSContext>(),
            "EVENTS",
            EventStreamSubjects
        ));
        builder.Services.AddSingleton<IHostedService>(sp => new StreamBootstrapperService(
            sp.GetRequiredService<NatsJSContext>(),
            "DELTAS",
            DeltaStreamSubjects
        ));
        builder.Services.AddSingleton<IHostedService>(sp => new StreamBootstrapperService(
            sp.GetRequiredService<NatsJSContext>(),
            "AUDITS",
            AuditStreamSubjects
        ));

        // Promoter (optional)
        var promoterEnabled = (
            Environment.GetEnvironmentVariable("PROMOTER_ENABLED") ?? "true"
        ).Equals("true", StringComparison.OrdinalIgnoreCase);
        if (promoterEnabled)
        {
            builder.Services.AddSingleton<IHostedService>(sp => new PromoterService(
                sp.GetRequiredService<NatsJSContext>(),
                sp.GetRequiredService<ILogger<PromoterService>>(),
                sp.GetRequiredService<AppSqliteFactory>()
            ));
        }

        // Database initialization - ensure database exists on startup
        builder.Services.AddSingleton<IHostedService>(sp =>
        {
            var dbFactory = sp.GetRequiredService<AppSqliteFactory>();
            var logger = sp.GetRequiredService<ILogger<DatabaseInitializerService>>();
            return new DatabaseInitializerService(dbFactory, sp, logger);
        });

        // Delta stream consumer for centralized processing
        builder.Services.AddSingleton<IHostedService, DeltaStreamConsumerService>();
    }
}
