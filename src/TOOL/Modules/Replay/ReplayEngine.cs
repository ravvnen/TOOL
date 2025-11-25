using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Dapper;
using Microsoft.Data.Sqlite;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;
using Infrastructure.Database;

namespace Modules.Replay;

/// <summary>
/// Stateless replay engine: reconstructs IM state from DELTAS stream
/// </summary>
public sealed class ReplayEngine
{
    private readonly NatsJSContext _js;
    private readonly AppSqliteFactory _dbFactory;
    private readonly ILogger<ReplayEngine> _log;

    public ReplayEngine(NatsJSContext js, AppSqliteFactory dbFactory, ILogger<ReplayEngine> log)
    {
        _js = js;
        _dbFactory = dbFactory;
        _log = log;
    }

    /// <summary>
    /// Replay DELTAS stream from beginning to reconstruct IM state in a fresh database
    /// </summary>
    /// <param name="ns">Namespace to replay</param>
    /// <param name="outputDbPath">Path to fresh SQLite database</param>
    /// <param name="maxSequence">Optional max sequence number (null = replay all)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Replay result with statistics and final im_hash</returns>
    public async Task<ReplayResult> ReplayAsync(
        string ns,
        string outputDbPath,
        ulong? maxSequence = null,
        CancellationToken ct = default
    )
    {
        var startedAt = DateTimeOffset.UtcNow;
        var sw = Stopwatch.StartNew();

        _log.LogInformation(
            "Replay started: ns={Ns}, outputDb={DbPath}, maxSeq={MaxSeq}",
            ns,
            outputDbPath,
            maxSequence?.ToString() ?? "latest"
        );

        // 1) Initialize fresh database with projection schema
        await using var db = _dbFactory.Open(outputDbPath);
        await InitializeDatabaseAsync(db, ct);

        // 2) Create ephemeral consumer (no durable name)
        //
        // WHY EPHEMERAL?
        // - Replay is stateless: each run starts from scratch (no progress tracking needed)
        // - Clean slate semantics: always replay from sequence 1 (DeliverPolicy.All)
        // - No state pollution: avoid cluttering NATS with replay consumer state
        // - Concurrency-safe: multiple concurrent replays don't conflict (each gets unique consumer)
        //
        // NOTE: Consumer durability ≠ Event persistence!
        // - DELTAS stream has unlimited retention (configured in StreamBootstrapper)
        // - Ephemeral consumer can read ALL events via DeliverPolicy.All
        // - Durable consumer would track position across runs (wrong for stateless replay)
        var consumerName = $"replay-{Guid.NewGuid():N}";
        var consumer = await _js.CreateConsumerAsync(
            "DELTAS",
            new ConsumerConfig
            {
                Name = consumerName,
                FilterSubject = $"delta.{ns}.>",
                DeliverPolicy = ConsumerConfigDeliverPolicy.All,
                AckPolicy = ConsumerConfigAckPolicy.Explicit,
                MaxAckPending = 256,
            },
            ct
        );

        _log.LogDebug("Created ephemeral consumer: {Consumer}", consumerName);

        int eventsProcessed = 0;
        int upserts = 0;
        int retracts = 0;

        try
        {
            // 3) Consume DELTAS from beginning
            await foreach (
                var msg in consumer
                    .FetchAsync<byte[]>(new NatsJSFetchOpts { MaxMsgs = 10000 })
                    .WithCancellation(ct)
            )
            {
                // Stop if we've reached maxSequence
                if (maxSequence.HasValue && msg.Metadata?.Sequence.Stream > maxSequence.Value)
                {
                    _log.LogDebug(
                        "Reached maxSequence={MaxSeq}, stopping replay",
                        maxSequence.Value
                    );
                    await msg.AckAsync(cancellationToken: ct);
                    break;
                }

                if (msg.Data is null || msg.Data.Length == 0)
                {
                    await msg.AckAsync(cancellationToken: ct);
                    continue;
                }

                // Parse DELTA event
                using var doc = JsonDocument.Parse(msg.Data);
                var root = doc.RootElement;

                if (
                    !TryGetString(root, "ns", out var eventNs)
                    || eventNs != ns
                    || !TryGetString(root, "item_id", out var itemId)
                    || !TryGetString(root, "policy_version", out var policyVersion)
                )
                {
                    _log.LogWarning(
                        "Invalid delta payload at seq={Seq}. Subject={Subject}",
                        msg.Metadata?.Sequence.Stream,
                        msg.Subject
                    );
                    await msg.AckAsync(cancellationToken: ct);
                    continue;
                }

                var type = GetStringOrEmpty(root, "type");
                var title = GetStringOrEmpty(root, "title");
                var content = GetStringOrEmpty(root, "content");
                var labels =
                    root.TryGetProperty("labels", out var labelsEl)
                    && labelsEl.ValueKind == JsonValueKind.Array
                        ? JsonSerializer.Serialize(
                            labelsEl
                                .EnumerateArray()
                                .Select(x => x.GetString())
                                .Where(x => !string.IsNullOrWhiteSpace(x))
                        )
                        : "[]";
                var newVersion = root.GetProperty("new_version").GetInt32();

                var occurredAt = TryGetDateTimeOffset(root, "occurred_at") ?? DateTimeOffset.UtcNow;
                var emittedAt = TryGetDateTimeOffset(root, "emitted_at") ?? DateTimeOffset.UtcNow;

                var source = root.GetProperty("source");
                var repo = GetStringOrEmpty(source, "repo");
                var srcRef = GetStringOrEmpty(source, "ref");
                var path = GetStringOrEmpty(source, "path");
                var blobSha = GetStringOrEmpty(source, "blob_sha");

                // Apply DELTA to fresh database (no transaction needed for single consumer)
                using var tx = db.BeginTransaction();

                if (type == "im.upsert.v1")
                {
                    await ApplyUpsertAsync(
                        db,
                        tx,
                        ns,
                        itemId,
                        newVersion,
                        title,
                        content,
                        labels,
                        policyVersion,
                        occurredAt,
                        emittedAt,
                        repo,
                        srcRef,
                        path,
                        blobSha
                    );
                    upserts++;
                }
                else if (type == "im.retract.v1")
                {
                    await ApplyRetractAsync(
                        db,
                        tx,
                        ns,
                        itemId,
                        newVersion,
                        title,
                        content,
                        labels,
                        policyVersion,
                        occurredAt,
                        emittedAt
                    );
                    retracts++;
                }

                tx.Commit();

                eventsProcessed++;
                await msg.AckAsync(cancellationToken: ct);

                // Log progress every 100 events
                if (eventsProcessed % 100 == 0)
                {
                    _log.LogDebug(
                        "Replay progress: {Count} events processed (upserts={U}, retracts={R})",
                        eventsProcessed,
                        upserts,
                        retracts
                    );
                }
            }
        }
        finally
        {
            // Delete ephemeral consumer
            try
            {
                await _js.DeleteConsumerAsync("DELTAS", consumerName, ct);
                _log.LogDebug("Deleted ephemeral consumer: {Consumer}", consumerName);
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Failed to delete ephemeral consumer (non-fatal)");
            }
        }

        // 4) Compute final state hash
        var (activeCount, imHash) = await ComputeStateHashAsync(db, ns);

        sw.Stop();
        var completedAt = DateTimeOffset.UtcNow;

        _log.LogInformation(
            "Replay completed: ns={Ns}, events={Count} (upserts={U}, retracts={R}), activeRules={Active}, hash={Hash}, durationMs={Ms}",
            ns,
            eventsProcessed,
            upserts,
            retracts,
            activeCount,
            imHash,
            sw.ElapsedMilliseconds
        );

        return new ReplayResult(
            ns,
            eventsProcessed,
            activeCount,
            imHash,
            sw.ElapsedMilliseconds,
            startedAt,
            completedAt
        );
    }

    // ===== Database Initialization =====

    private static async Task InitializeDatabaseAsync(SqliteConnection db, CancellationToken ct)
    {
        const string ddl = """
            PRAGMA journal_mode = WAL;
            PRAGMA busy_timeout = 3000;
            PRAGMA synchronous = NORMAL;  -- Fast replay mode

            CREATE TABLE IF NOT EXISTS im_items_current (
                ns              TEXT NOT NULL,
                item_id         TEXT NOT NULL,
                version         INTEGER NOT NULL,
                title           TEXT NOT NULL,
                content         TEXT NOT NULL,
                labels_json     TEXT NOT NULL,
                is_active       INTEGER NOT NULL,
                policy_version  TEXT NOT NULL,
                occurred_at     TEXT NOT NULL,
                emitted_at      TEXT NOT NULL,
                PRIMARY KEY (ns, item_id)
            );

            CREATE TABLE IF NOT EXISTS im_items_history (
                ns              TEXT NOT NULL,
                item_id         TEXT NOT NULL,
                version         INTEGER NOT NULL,
                title           TEXT NOT NULL,
                content         TEXT NOT NULL,
                labels_json     TEXT NOT NULL,
                is_active       INTEGER NOT NULL,
                policy_version  TEXT NOT NULL,
                occurred_at     TEXT NOT NULL,
                emitted_at      TEXT NOT NULL,
                PRIMARY KEY (ns, item_id, version)
            );

            CREATE TABLE IF NOT EXISTS source_bindings (
                ns              TEXT NOT NULL,
                item_id         TEXT NOT NULL,
                version         INTEGER NOT NULL,
                repo            TEXT NOT NULL,
                ref             TEXT NOT NULL,
                path            TEXT NOT NULL,
                blob_sha        TEXT NOT NULL,
                PRIMARY KEY (ns, item_id, version)
            );
            """;
        await db.ExecuteAsync(ddl);
    }

    // ===== DELTA Application Logic (same as DeltaConsumer, but without idempotency) =====

    private static async Task ApplyUpsertAsync(
        SqliteConnection db,
        SqliteTransaction tx,
        string ns,
        string itemId,
        int version,
        string title,
        string content,
        string labels,
        string policyVersion,
        DateTimeOffset occurredAt,
        DateTimeOffset emittedAt,
        string repo,
        string srcRef,
        string path,
        string blobSha
    )
    {
        // Update current snapshot
        await db.ExecuteAsync(
            @"
            INSERT INTO im_items_current
              (ns,item_id,version,title,content,labels_json,is_active,policy_version,occurred_at,emitted_at)
            VALUES
              (@Ns,@ItemId,@Version,@Title,@Content,@Labels,1,@Policy,@OccurredAt,@EmittedAt)
            ON CONFLICT(ns,item_id) DO UPDATE SET
              version=excluded.version,
              title=excluded.title,
              content=excluded.content,
              labels_json=excluded.labels_json,
              is_active=excluded.is_active,
              policy_version=excluded.policy_version,
              occurred_at=excluded.occurred_at,
              emitted_at=excluded.emitted_at",
            new
            {
                Ns = ns,
                ItemId = itemId,
                Version = version,
                Title = title,
                Content = content,
                Labels = labels,
                Policy = policyVersion,
                OccurredAt = occurredAt.ToString("o"),
                EmittedAt = emittedAt.ToString("o"),
            },
            tx
        );

        // History
        await db.ExecuteAsync(
            @"
            INSERT INTO im_items_history
              (ns,item_id,version,title,content,labels_json,is_active,policy_version,occurred_at,emitted_at)
            VALUES
              (@Ns,@ItemId,@Version,@Title,@Content,@Labels,1,@Policy,@OccurredAt,@EmittedAt)",
            new
            {
                Ns = ns,
                ItemId = itemId,
                Version = version,
                Title = title,
                Content = content,
                Labels = labels,
                Policy = policyVersion,
                OccurredAt = occurredAt.ToString("o"),
                EmittedAt = emittedAt.ToString("o"),
            },
            tx
        );

        // Source binding
        await db.ExecuteAsync(
            @"
            INSERT OR REPLACE INTO source_bindings
              (ns,item_id,version,repo,ref,path,blob_sha)
            VALUES
              (@Ns,@ItemId,@Version,@Repo,@Ref,@Path,@BlobSha)",
            new
            {
                Ns = ns,
                ItemId = itemId,
                Version = version,
                Repo = repo,
                Ref = srcRef,
                Path = path,
                BlobSha = blobSha,
            },
            tx
        );
    }

    private static async Task ApplyRetractAsync(
        SqliteConnection db,
        SqliteTransaction tx,
        string ns,
        string itemId,
        int version,
        string title,
        string content,
        string labels,
        string policyVersion,
        DateTimeOffset occurredAt,
        DateTimeOffset emittedAt
    )
    {
        // Mark inactive in current
        await db.ExecuteAsync(
            @"
            UPDATE im_items_current
            SET version=@Version, is_active=0,
                policy_version=@Policy,
                occurred_at=@OccurredAt, emitted_at=@EmittedAt
            WHERE ns=@Ns AND item_id=@ItemId",
            new
            {
                Ns = ns,
                ItemId = itemId,
                Version = version,
                Policy = policyVersion,
                OccurredAt = occurredAt.ToString("o"),
                EmittedAt = emittedAt.ToString("o"),
            },
            tx
        );

        // History
        await db.ExecuteAsync(
            @"
            INSERT INTO im_items_history
              (ns,item_id,version,title,content,labels_json,is_active,policy_version,occurred_at,emitted_at)
            VALUES
              (@Ns,@ItemId,@Version,@Title,@Content,@Labels,0,@Policy,@OccurredAt,@EmittedAt)",
            new
            {
                Ns = ns,
                ItemId = itemId,
                Version = version,
                Title = title,
                Content = content,
                Labels = labels,
                Policy = policyVersion,
                OccurredAt = occurredAt.ToString("o"),
                EmittedAt = emittedAt.ToString("o"),
            },
            tx
        );
    }

    // ===== Hash Computation =====

    private static async Task<(long ActiveCount, string ImHash)> ComputeStateHashAsync(
        SqliteConnection db,
        string ns
    )
    {
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

    // ===== JSON Helpers =====

    private static bool TryGetString(JsonElement el, string name, out string value)
    {
        value = "";
        if (el.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String)
        {
            value = p.GetString() ?? "";
            return true;
        }
        return false;
    }

    private static string GetStringOrEmpty(JsonElement el, string name) =>
        el.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String
            ? (p.GetString() ?? "")
            : "";

    private static DateTimeOffset? TryGetDateTimeOffset(JsonElement el, string name)
    {
        if (
            el.TryGetProperty(name, out var p)
            && p.ValueKind == JsonValueKind.String
            && DateTimeOffset.TryParse(p.GetString(), out var dto)
        )
            return dto;
        return null;
    }
}
