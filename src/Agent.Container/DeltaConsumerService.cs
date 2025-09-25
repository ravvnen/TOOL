using System.Text.Json;
using Dapper;
using Microsoft.Data.Sqlite;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;

namespace Agent.Container;

public sealed class DeltaConsumerService : BackgroundService
{
    private readonly NatsJSContext _js;
    private readonly AgentConfig _cfg;
    private readonly AppSqliteFactory _sf;
    private readonly ILogger<DeltaConsumerService> _log;

    public DeltaConsumerService(NatsJSContext js, AgentConfig cfg, AppSqliteFactory sf, ILogger<DeltaConsumerService> log)
    {
        _js = js; _cfg = cfg; _sf = sf; _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using var db = _sf.Open(_cfg.DbPath);

        // --- Ensure schema exists (idempotent) ---
        await db.ExecuteAsync("""
            PRAGMA journal_mode = WAL;
            PRAGMA busy_timeout = 3000;

            CREATE TABLE IF NOT EXISTS im_items_current (
              ns TEXT NOT NULL,
              item_id TEXT NOT NULL,
              version INTEGER NOT NULL,
              title TEXT NOT NULL,
              content TEXT NOT NULL,
              labels_json TEXT NOT NULL,
              is_active INTEGER NOT NULL,
              policy_version TEXT NOT NULL,
              occurred_at TEXT NOT NULL,
              emitted_at TEXT NOT NULL,
              PRIMARY KEY (ns, item_id)
            );

            CREATE TABLE IF NOT EXISTS im_items_history (
              ns TEXT NOT NULL,
              item_id TEXT NOT NULL,
              version INTEGER NOT NULL,
              title TEXT NOT NULL,
              content TEXT NOT NULL,
              labels_json TEXT NOT NULL,
              is_active INTEGER NOT NULL,
              policy_version TEXT NOT NULL,
              occurred_at TEXT NOT NULL,
              emitted_at TEXT NOT NULL,
              PRIMARY KEY (ns, item_id, version)
            );

            CREATE TABLE IF NOT EXISTS source_bindings (
              ns TEXT NOT NULL,
              item_id TEXT NOT NULL,
              version INTEGER NOT NULL,
              repo TEXT NOT NULL,
              ref TEXT NOT NULL,
              path TEXT NOT NULL,
              blob_sha TEXT NOT NULL,
              PRIMARY KEY (ns, item_id, version)
            );

            -- NEW: idempotency table (one row per applied delta)
            CREATE TABLE IF NOT EXISTS agent_seen_deltas (
              ns TEXT NOT NULL,
              input_event_id TEXT NOT NULL,
              PRIMARY KEY (ns, input_event_id)
            );
        """);

        var durable = $"agent-{_cfg.AgentName}";
        _log.LogInformation("Agent: starting DeltaConsumerService ns={ns} durable={durable} nats={url}", _cfg.Ns, durable, _cfg.NatsUrl);

        var consumer = await _js.CreateOrUpdateConsumerAsync("DELTAS", new ConsumerConfig {
            DurableName = durable, Name = durable,
            FilterSubject = "delta.>",
            DeliverPolicy = ConsumerConfigDeliverPolicy.All,
            AckPolicy = ConsumerConfigAckPolicy.Explicit,
            MaxAckPending = 1024
        }, stoppingToken);

        _log.LogInformation("Agent: consuming DELTAS as durable={durable}", durable);

        await foreach (var msg in consumer.ConsumeAsync<byte[]>().WithCancellation(stoppingToken))
        {
            try
            {
                using var doc = JsonDocument.Parse(msg.Data);
                var root = doc.RootElement;

                var type = root.GetProperty("type").GetString();
                var ns   = root.GetProperty("ns").GetString();

                // Only keep items for our namespace
                if (!string.Equals(ns, _cfg.Ns, StringComparison.OrdinalIgnoreCase))
                {
                    _log.LogDebug("Agent: skip delta ns={ns} (agent ns={cfg})", ns, _cfg.Ns);
                    await msg.AckAsync();
                    continue;
                }

                var itemId        = root.GetProperty("item_id").GetString()!;
                var version       = root.GetProperty("new_version").GetInt32();
                var policyVersion = root.GetProperty("policy_version").GetString() ?? "unknown";
                var occurredAt    = root.TryGetProperty("occurred_at", out var occ) && occ.ValueKind==JsonValueKind.String ? occ.GetString()! : DateTimeOffset.UtcNow.ToString("o");
                var emittedAt     = root.TryGetProperty("emitted_at", out var emi) && emi.ValueKind==JsonValueKind.String ? emi.GetString()! : DateTimeOffset.UtcNow.ToString("o");
                var inputEventId  = root.TryGetProperty("input_event_id", out var ie) && ie.ValueKind==JsonValueKind.String ? ie.GetString()! : $"{ns}:{itemId}:v{version}:{type}";

                // ---- Idempotency barrier: if we've applied this delta, skip safely
                var seen = await db.ExecuteScalarAsync<long>(
                    "SELECT COUNT(1) FROM agent_seen_deltas WHERE ns=@ns AND input_event_id=@id",
                    new { ns, id = inputEventId });

                if (seen == 1)
                {
                    _log.LogDebug("Agent: idempotent-skip input_event_id={id} item={item} v={v} type={type}", inputEventId, itemId, version, type);
                    await msg.AckAsync();
                    continue;
                }

                if (type == "im.upsert.v1")
                {
                    var title   = root.GetProperty("title").GetString() ?? "";
                    var content = root.GetProperty("content").GetString() ?? "";
                    var labels  = root.TryGetProperty("labels", out var lab) && lab.ValueKind==JsonValueKind.Array
                                    ? JsonSerializer.Serialize(lab.EnumerateArray().Select(x => x.GetString()))
                                    : "[]";

                    using var tx = db.BeginTransaction();

                    // Current snapshot
                    await db.ExecuteAsync(@"
                        INSERT INTO im_items_current (ns,item_id,version,title,content,labels_json,is_active,policy_version,occurred_at,emitted_at)
                        VALUES (@ns,@id,@v,@t,@c,@l,1,@p,@o,@e)
                        ON CONFLICT(ns,item_id) DO UPDATE SET
                            version=excluded.version,
                            title=excluded.title,
                            content=excluded.content,
                            labels_json=excluded.labels_json,
                            is_active=excluded.is_active,
                            policy_version=excluded.policy_version,
                            occurred_at=excluded.occurred_at,
                            emitted_at=excluded.emitted_at
                    ", new { ns, id = itemId, v = version, t = title, c = content, l = labels, p = policyVersion, o = occurredAt, e = emittedAt }, tx);

                    // History (idempotent)
                    await db.ExecuteAsync(@"
                        INSERT OR IGNORE INTO im_items_history (ns,item_id,version,title,content,labels_json,is_active,policy_version,occurred_at,emitted_at)
                        VALUES (@ns,@id,@v,@t,@c,@l,1,@p,@o,@e)
                    ", new { ns, id = itemId, v = version, t = title, c = content, l = labels, p = policyVersion, o = occurredAt, e = emittedAt }, tx);

                    // Provenance
                    if (root.TryGetProperty("source", out var src) && src.ValueKind == JsonValueKind.Object)
                    {
                        await db.ExecuteAsync(@"
                            INSERT OR REPLACE INTO source_bindings (ns,item_id,version,repo,ref,path,blob_sha)
                            VALUES (@ns,@id,@v,@repo,@ref,@path,@blob)
                        ", new {
                            ns, id = itemId, v = version,
                            repo = src.TryGetProperty("repo", out var repoEl) ? repoEl.GetString() ?? "" : (src.TryGetProperty("repo_url", out var repoUrl) ? repoUrl.GetString() ?? "" : ""),
                            @ref = src.TryGetProperty("ref", out var refEl) ? refEl.GetString() ?? "" : "",
                            path = src.TryGetProperty("path", out var pathEl) ? pathEl.GetString() ?? "" : "",
                            blob = src.TryGetProperty("blob_sha", out var blobEl) ? blobEl.GetString() ?? "" : ""
                        }, tx);
                    }

                    // Mark seen only after all mutations succeed
                    await db.ExecuteAsync("INSERT OR IGNORE INTO agent_seen_deltas(ns,input_event_id) VALUES(@ns,@id)",
                        new { ns, id = inputEventId }, tx);

                    tx.Commit();
                    _log.LogInformation("Agent: applied UPSERT item={item} v={v} input={id}", itemId, version, inputEventId);
                }
                else if (type == "im.retract.v1")
                {
                    using var tx = db.BeginTransaction();

                    // Load current to write meaningful history; if missing, create a tombstone title/content
                    var cur = await db.QueryFirstOrDefaultAsync<(string Title,string Content,string Labels)>(
                        "SELECT title AS Title, content AS Content, labels_json AS Labels FROM im_items_current WHERE ns=@ns AND item_id=@id",
                        new { ns, id = itemId }, tx);

                    var title   = cur.Title   ?? "(tombstone)";
                    var content = cur.Content ?? "";
                    var labels  = cur.Labels  ?? "[]";

                    // Ensure there is a current row to flip inactive (insert if not exists)
                    await db.ExecuteAsync(@"
                        INSERT INTO im_items_current (ns,item_id,version,title,content,labels_json,is_active,policy_version,occurred_at,emitted_at)
                        VALUES (@ns,@id,@v,@t,@c,@l,0,@p,@o,@e)
                        ON CONFLICT(ns,item_id) DO UPDATE SET
                            version=excluded.version,
                            is_active=0,
                            policy_version=excluded.policy_version,
                            occurred_at=excluded.occurred_at,
                            emitted_at=excluded.emitted_at
                    ", new { ns, id = itemId, v = version, t = title, c = content, l = labels, p = policyVersion, o = occurredAt, e = emittedAt }, tx);

                    // History (idempotent)
                    await db.ExecuteAsync(@"
                        INSERT OR IGNORE INTO im_items_history (ns,item_id,version,title,content,labels_json,is_active,policy_version,occurred_at,emitted_at)
                        VALUES (@ns,@id,@v,@t,@c,@l,0,@p,@o,@e)
                    ", new { ns, id = itemId, v = version, t = title, c = content, l = labels, p = policyVersion, o = occurredAt, e = emittedAt }, tx);

                    await db.ExecuteAsync("INSERT OR IGNORE INTO agent_seen_deltas(ns,input_event_id) VALUES(@ns,@id)",
                        new { ns, id = inputEventId }, tx);

                    tx.Commit();
                    _log.LogInformation("Agent: applied RETRACT item={item} v={v} input={id}", itemId, version, inputEventId);
                }
                else
                {
                    _log.LogWarning("Agent: unknown delta type={type} item={item}", type, itemId);
                }

                await msg.AckAsync();
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _log.LogError(ex, "Agent DeltaConsumer error");
                try { await msg.NakAsync(); } catch {}
            }
        }

        _log.LogInformation("Agent: DeltaConsumerService shutting down ns={ns} durable={durable}", _cfg.Ns, durable);
    }
}
