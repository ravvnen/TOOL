using Microsoft.Data.Sqlite;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;

namespace TOOL.Modules.DeltaProjection;

/// <summary>
/// Background service that consumes DELTA events from NATS JetStream
/// and applies them to the projection database.
/// Orchestrates DeltaParser and DeltaProjector.
/// </summary>
public sealed class DeltaStreamConsumerService : BackgroundService
{
    private readonly NatsJSContext _js;
    private readonly ILogger<DeltaStreamConsumerService> _log;
    private readonly DeltaProjector _projector;
    private readonly string _dbPath;
    private readonly string _durable;

    public DeltaStreamConsumerService(NatsJSContext js, ILogger<DeltaStreamConsumerService> log)
    {
        _js = js;
        _log = log;
        _projector = new DeltaProjector();
        _dbPath = Environment.GetEnvironmentVariable("RULES_DB_PATH") ?? "rules.db";
        _durable = Environment.GetEnvironmentVariable("DELTA_DURABLE") ?? "delta-consumer";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Create database connection
        await using var db = new SqliteConnection($"Data Source={_dbPath}");
        await db.OpenAsync(stoppingToken);

        // Initialize database schema
        await _projector.InitializeSchemaAsync(db, stoppingToken);

        // Create NATS consumer
        var consumer = await _js.CreateOrUpdateConsumerAsync(
            "DELTAS",
            new ConsumerConfig
            {
                DurableName = _durable,
                Name = _durable,
                FilterSubject = "delta.>",
                DeliverPolicy = ConsumerConfigDeliverPolicy.All,
                AckPolicy = ConsumerConfigAckPolicy.Explicit,
                MaxAckPending = 256,
            },
            stoppingToken
        );

        _log.LogInformation(
            "DeltaStreamConsumerService started: Stream=DELTAS Durable={Durable}",
            _durable
        );

        // Consume messages
        await foreach (var msg in consumer.ConsumeAsync<byte[]>().WithCancellation(stoppingToken))
        {
            try
            {
                // Validate message
                if (msg.Data is null || msg.Data.Length == 0)
                {
                    _log.LogWarning("Empty payload. Subject={Subject}", msg.Subject);
                    await msg.AckAsync();
                    continue;
                }

                // Parse delta event
                var delta = DeltaParser.Parse(msg.Data);
                if (delta is null)
                {
                    _log.LogWarning("Failed to parse delta. Subject={Subject}", msg.Subject);
                    await msg.AckAsync();
                    continue;
                }

                // Apply to projection
                await _projector.ApplyAsync(db, delta, stoppingToken);

                await msg.AckAsync();

                _log.LogInformation(
                    "Applied {Type} ns={Ns} item={Item} v{V}",
                    delta.Type,
                    delta.Ns,
                    delta.ItemId,
                    delta.NewVersion
                );
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Error processing delta. Subject={Subject}", msg.Subject);
                await msg.NakAsync();
            }
        }
    }
}
