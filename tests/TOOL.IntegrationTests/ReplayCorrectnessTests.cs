using System.Text;
using Dapper;
using Microsoft.Data.Sqlite;
using NATS.Client.Core;
using NATS.Client.JetStream;
using TOOL.Modules.Replay;
using Xunit;

namespace TOOL.IntegrationTests;

/// <summary>
/// Integration tests for replay correctness (H3: Replayability & Determinism)
/// </summary>
public class ReplayCorrectnessTests : IAsyncLifetime
{
    private NatsConnection? _nats;
    private NatsJSContext? _js;
    private ReplayEngine? _replayEngine;
    private readonly string _ns = "ravvnen.consulting";
    private readonly string _liveDbPath;

    public ReplayCorrectnessTests()
    {
        _liveDbPath = Environment.GetEnvironmentVariable("RULES_DB_PATH") ?? "rules.db";
    }

    public async Task InitializeAsync()
    {
        try
        {
            var natsUrl = Environment.GetEnvironmentVariable("NATS_URL") ?? "nats://localhost:4222";
            _nats = new NatsConnection(new NatsOpts { Url = natsUrl });
            await _nats.ConnectAsync();
            _js = new NatsJSContext(_nats);

            var logger = new TestLogger<ReplayEngine>();
            _replayEngine = new ReplayEngine(_js, logger);
        }
        catch (Exception ex)
        {
            // If NATS is not available, tests will be skipped
            Console.WriteLine($"NATS initialization failed (tests will be skipped): {ex.Message}");
        }
    }

    public async Task DisposeAsync()
    {
        if (_nats is not null)
        {
            await _nats.DisposeAsync();
        }
    }

    [Fact]
    public async Task FullReplay_ProducesSameStateHash()
    {
        // Skip if NATS is not available
        if (_replayEngine is null || _js is null)
        {
            Console.WriteLine("NATS unavailable, skipping test");
            return;
        }

        // Skip if live database doesn't exist
        if (!File.Exists(_liveDbPath))
        {
            Console.WriteLine($"Live database not found at {_liveDbPath}, skipping test");
            return;
        }

        // Arrange: Snapshot current live state
        var (liveCount, liveHash) = await GetStateHashAsync(_liveDbPath, _ns);

        Console.WriteLine($"Live state: activeCount={liveCount}, hash={liveHash}");

        // Act: Replay from scratch to temporary database
        var replayDbPath = Path.Combine(Path.GetTempPath(), $"replay_test_{Guid.NewGuid():N}.db");

        try
        {
            var result = await _replayEngine.ReplayAsync(_ns, replayDbPath);

            Console.WriteLine(
                $"Replay result: events={result.EventsProcessed}, activeCount={result.ActiveCount}, hash={result.ImHash}, durationMs={result.ReplayTimeMs}"
            );

            // Assert: State Reconstruction Accuracy (SRA) = 1.0
            Assert.Equal(liveHash, result.ImHash);
            Assert.Equal(liveCount, result.ActiveCount);

            // Additional assertions
            Assert.True(result.EventsProcessed > 0, "Replay should process at least one event");
            Assert.True(result.ReplayTimeMs > 0, "Replay duration should be greater than zero");
        }
        finally
        {
            // Cleanup: Remove temporary replay database
            if (File.Exists(replayDbPath))
            {
                File.Delete(replayDbPath);
            }
        }
    }

    [Fact]
    public async Task MultipleReplays_ProduceSameHash()
    {
        // Skip if NATS is not available
        if (_replayEngine is null || _js is null)
        {
            Console.WriteLine("NATS unavailable, skipping test");
            return;
        }

        // Skip if live database doesn't exist
        if (!File.Exists(_liveDbPath))
        {
            Console.WriteLine($"Live database not found at {_liveDbPath}, skipping test");
            return;
        }

        // Arrange: Run multiple replays
        const int trials = 3;
        var results = new List<(long ActiveCount, string ImHash)>();

        for (int i = 0; i < trials; i++)
        {
            var replayDbPath = Path.Combine(
                Path.GetTempPath(),
                $"replay_trial_{i}_{Guid.NewGuid():N}.db"
            );

            try
            {
                var result = await _replayEngine!.ReplayAsync(_ns, replayDbPath);
                results.Add((result.ActiveCount, result.ImHash));

                Console.WriteLine(
                    $"Trial {i + 1}: events={result.EventsProcessed}, activeCount={result.ActiveCount}, hash={result.ImHash}"
                );
            }
            finally
            {
                if (File.Exists(replayDbPath))
                {
                    File.Delete(replayDbPath);
                }
            }
        }

        // Assert: All replays produce identical state
        var firstHash = results[0].ImHash;
        var firstCount = results[0].ActiveCount;

        foreach (var (count, hash) in results)
        {
            Assert.Equal(firstHash, hash);
            Assert.Equal(firstCount, count);
        }

        Console.WriteLine($"✅ All {trials} trials produced identical state (SRA = 1.00)");
    }

    // ===== Helper: Compute state hash from database =====

    private static async Task<(long ActiveCount, string ImHash)> GetStateHashAsync(
        string dbPath,
        string ns
    )
    {
        await using var db = new SqliteConnection($"Data Source={dbPath}");
        await db.OpenAsync();

        var count = await db.ExecuteScalarAsync<long>(
            "SELECT COUNT(*) FROM im_items_current WHERE ns=@ns AND is_active=1",
            new { ns }
        );

        var rows = await db.QueryAsync<(string Title, string Content)>(
            "SELECT title, content FROM im_items_current WHERE ns=@ns AND is_active=1 ORDER BY item_id",
            new { ns }
        );

        var text = string.Join("\n---\n", rows.Select(r => r.Title + "\n" + r.Content));
        var hash = HexSha256(text);

        return (count, hash);
    }

    private static string HexSha256(string s)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(s)));
    }
}

/// <summary>
/// Simple test logger that writes to console
/// </summary>
internal class TestLogger<T> : Microsoft.Extensions.Logging.ILogger<T>
{
    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => true;

    public void Log<TState>(
        Microsoft.Extensions.Logging.LogLevel logLevel,
        Microsoft.Extensions.Logging.EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter
    )
    {
        Console.WriteLine($"[{logLevel}] {formatter(state, exception)}");
        if (exception is not null)
        {
            Console.WriteLine(exception.ToString());
        }
    }
}
