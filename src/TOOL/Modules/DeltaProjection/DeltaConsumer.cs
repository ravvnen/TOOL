using System.Text.Json;
using Dapper;
using Microsoft.Data.Sqlite;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;

namespace TOOL.Modules.DeltaProjection;

public sealed class DeltaConsumer : BackgroundService
{
    private readonly NatsJSContext _js;
    private readonly ILogger<DeltaConsumer> _log;
    private readonly string _dbPath;
    private readonly string _durable;

    public DeltaConsumer(NatsJSContext js, ILogger<DeltaConsumer> log)
    {
        _js = js;
        _log = log;
        _dbPath = Environment.GetEnvironmentVariable("RULES_DB_PATH") ?? "rules.db";
        _durable = Environment.GetEnvironmentVariable("DELTA_DURABLE") ?? "delta-consumer";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // 1) DB init
        await using var db = new SqliteConnection($"Data Source={_dbPath}");
        await db.OpenAsync(stoppingToken);

        const string ddl = """
            PRAGMA journal_mode = WAL;
            PRAGMA busy_timeout = 3000;

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

            CREATE TABLE IF NOT EXISTS deltas_seen_events (
                ns       TEXT NOT NULL,
                event_id TEXT NOT NULL,
                PRIMARY KEY (ns, event_id)
            );

            -- FTS5 for MemoryCompiler integration
            CREATE VIRTUAL TABLE IF NOT EXISTS im_fts USING fts5(
                ns, item_id, title, content,
                content='im_items_current',
                content_rowid='rowid'
            );

            -- FTS5 triggers to keep search index in sync
            CREATE TRIGGER IF NOT EXISTS im_fts_insert AFTER INSERT ON im_items_current BEGIN
                INSERT INTO im_fts(rowid, ns, item_id, title, content)
                VALUES (new.rowid, new.ns, new.item_id, new.title, new.content);
            END;

            CREATE TRIGGER IF NOT EXISTS im_fts_delete AFTER DELETE ON im_items_current BEGIN
                INSERT INTO im_fts(im_fts, rowid, ns, item_id, title, content)
                VALUES ('delete', old.rowid, old.ns, old.item_id, old.title, old.content);
            END;

            CREATE TRIGGER IF NOT EXISTS im_fts_update AFTER UPDATE ON im_items_current BEGIN
                INSERT INTO im_fts(im_fts, rowid, ns, item_id, title, content)
                VALUES ('delete', old.rowid, old.ns, old.item_id, old.title, old.content);
                INSERT INTO im_fts(rowid, ns, item_id, title, content)
                VALUES (new.rowid, new.ns, new.item_id, new.title, new.content);
            END;
            """;
        await db.ExecuteAsync(ddl);

        // 2) Consumer on DELTAS
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

        _log.LogInformation("DeltaConsumer started: Stream=DELTAS Durable={Durable}", _durable);

        // 3) Consume DELTAS
        await foreach (var msg in consumer.ConsumeAsync<byte[]>().WithCancellation(stoppingToken))
        {
            try
            {
                if (msg.Data is null || msg.Data.Length == 0)
                {
                    _log.LogWarning("Empty payload. Subject={Subject}", msg.Subject);
                    await msg.AckAsync();
                    continue;
                }

                using var doc = JsonDocument.Parse(msg.Data);
                var root = doc.RootElement;

                if (
                    !TryGetString(root, "ns", out var ns)
                    || !TryGetString(root, "item_id", out var itemId)
                    || !TryGetString(root, "policy_version", out var policyVersion)
                )
                {
                    _log.LogWarning("Invalid delta payload. Subject={Subject}", msg.Subject);
                    await msg.NakAsync();
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
                var baseVersion =
                    root.TryGetProperty("base_version", out var bv)
                    && bv.ValueKind == JsonValueKind.Number
                        ? bv.GetInt32()
                        : 0;

                var occurredAt = TryGetDateTimeOffset(root, "occurred_at") ?? DateTimeOffset.UtcNow;
                var emittedAt = TryGetDateTimeOffset(root, "emitted_at") ?? DateTimeOffset.UtcNow;

                var source = root.GetProperty("source");
                var repo = GetStringOrEmpty(source, "repo");
                var srcRef = GetStringOrEmpty(source, "ref");
                var path = GetStringOrEmpty(source, "path");
                var blobSha = GetStringOrEmpty(source, "blob_sha");

                var inputEventId = GetStringOrEmpty(root, "input_event_id");

                // Idempotency
                var alreadySeen =
                    await db.ExecuteScalarAsync<long>(
                        "SELECT COUNT(1) FROM deltas_seen_events WHERE ns=@Ns AND event_id=@Eid",
                        new { Ns = ns, Eid = inputEventId }
                    ) > 0;

                if (alreadySeen)
                {
                    _log.LogDebug(
                        "Seen delta (ACK fast). ns={Ns} item={Item} id={Id}",
                        ns,
                        itemId,
                        inputEventId
                    );
                    await msg.AckAsync();
                    continue;
                }

                using var tx = db.BeginTransaction();

                if (type == "im.upsert.v1")
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
                            Version = newVersion,
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
                            Version = newVersion,
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
                            Version = newVersion,
                            Repo = repo,
                            Ref = srcRef,
                            Path = path,
                            BlobSha = blobSha,
                        },
                        tx
                    );
                }
                else if (type == "im.retract.v1")
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
                            Version = newVersion,
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
                            Version = newVersion,
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

                // Mark seen
                await db.ExecuteAsync(
                    "INSERT OR IGNORE INTO deltas_seen_events(ns,event_id) VALUES(@Ns,@Eid)",
                    new { Ns = ns, Eid = inputEventId },
                    tx
                );

                tx.Commit();

                _log.LogInformation(
                    "Applied {Type} ns={Ns} item={Item} v{V}",
                    type,
                    ns,
                    itemId,
                    newVersion
                );
                await msg.AckAsync();
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Error applying DELTA. Subject={Subject}", msg.Subject);
                try
                {
                    await msg.NakAsync();
                }
                catch { }
            }
        }
    }

    // ===== Helpers =====
    static bool TryGetString(JsonElement el, string name, out string value)
    {
        value = "";
        if (el.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String)
        {
            value = p.GetString() ?? "";
            return true;
        }
        return false;
    }

    static string GetStringOrEmpty(JsonElement el, string name) =>
        el.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String
            ? (p.GetString() ?? "")
            : "";

    static DateTimeOffset? TryGetDateTimeOffset(JsonElement el, string name)
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
