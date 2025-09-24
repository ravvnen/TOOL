using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Dapper;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;
using NATS.Client.Core;

public sealed class Promoter : BackgroundService
{
    private readonly NatsJSContext _js;
    private readonly ILogger<Promoter> _log;
    private readonly string _dbPath;
    private readonly string _durable;

    public Promoter(NatsJSContext js, ILogger<Promoter> log)
    {
        _js = js;
        _log = log;
        _dbPath  = Environment.GetEnvironmentVariable("PROMOTER_DB_PATH") ?? "promoter.db";
        _durable = Environment.GetEnvironmentVariable("PROMOTER_DURABLE") ?? "promoter-seed";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // 1) DB init
        await using var db = new SqliteConnection($"Data Source={_dbPath}");
        await db.OpenAsync(stoppingToken);

        const string ddl = """
            PRAGMA journal_mode = WAL;
            PRAGMA busy_timeout = 3000;

            CREATE TABLE IF NOT EXISTS promoter_versions (
                item_id        TEXT PRIMARY KEY,
                version        INTEGER NOT NULL,
                content_hash   TEXT NOT NULL,
                canonical_json TEXT NOT NULL,
                last_sha       TEXT NOT NULL,
                updated_at     TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS seen_events (
                event_id TEXT PRIMARY KEY
            );
            """;
        await db.ExecuteAsync(ddl);

        // 2) Durable consumer on EVENTS for seed proposals
        var filter = "evt.*.*.seed.proposal";
        // NOTE: Some NATS servers require the consumer Name (if set) to match DurableName for durable pull consumers.
        // We set both to the same value to avoid 'consumer name in subject does not match durable name' errors when reusing.
        var consumer = await _js.CreateOrUpdateConsumerAsync("EVENTS", new ConsumerConfig
        {
            DurableName   = _durable,
            Name          = _durable,
            FilterSubject = filter,
            DeliverPolicy = ConsumerConfigDeliverPolicy.All,
            AckPolicy     = ConsumerConfigAckPolicy.Explicit
        }, stoppingToken);

    _log.LogInformation("Promoter started: Stream=EVENTS Filter={Filter} Durable={Durable}", filter, _durable);
    _log.LogDebug("[Promoter] DB path {DbPath}", _dbPath);

        // 3) Consume
    await foreach (var msg in consumer.ConsumeAsync<byte[]>().WithCancellation(stoppingToken))
        {
            try
            {
        _log.LogDebug("[Promoter] Received Subject={Subject} DataLength={Length} Headers={HasHeaders}", msg.Subject, msg.Data?.Length, msg.Headers is not null);
                using var doc = JsonDocument.Parse(msg.Data);
                var root = doc.RootElement;

                // Required fields
                if (!root.TryGetProperty("item_id", out var itemIdEl) || string.IsNullOrWhiteSpace(itemIdEl.GetString()))
                {
                    _log.LogWarning("Missing item_id. Subject={Subject}", msg.Subject);
                    await msg.NakAsync();
                    continue;
                }
                var itemId = itemIdEl.GetString()!;

                if (!root.TryGetProperty("sha", out var shaEl) || string.IsNullOrWhiteSpace(shaEl.GetString()))
                {
                    _log.LogWarning("Missing sha. Subject={Subject}", msg.Subject);
                    await msg.NakAsync();
                    continue;
                }
                var sha = shaEl.GetString()!;

                // Parse org/repo from subject: evt.{org}.{repo}.seed.proposal
                var tokens = msg.Subject.Split('.', StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length < 5)
                {
                    _log.LogWarning("Unexpected subject shape: {Subject}", msg.Subject);
                    await msg.NakAsync();
                    continue;
                }
                var org = tokens[1];
                var repo = tokens[2];

                // ---- 4) POLICY GATE (explicit) ----
                var decision = EvaluatePolicy(root);
                _log.LogDebug("[Promoter] Policy decision={Decision} action={Action} item_id={ItemId} sha={Sha}", decision.Kind, decision.Action, itemId, sha);
                if (decision.Kind == PromotionKind.Skip)
                {
                    _log.LogInformation("Skip: {Reason} (item_id={ItemId}, sha={Sha})", decision.Reason, itemId, sha);
                    await msg.AckAsync();
                    continue;
                }
                if (decision.Kind == PromotionKind.Defer)
                {
                    _log.LogInformation("Defer: {Reason} (item_id={ItemId}, sha={Sha})", decision.Reason, itemId, sha);
                    await msg.NakAsync(); // transient condition — try again later
                    continue;
                }
                // else Promote → continue

                // ---- 5) Idempotency (seen_events) ----
                var seenKey = $"{msg.Subject}:{itemId}:{sha}";
                var alreadySeen = await db.ExecuteScalarAsync<long>(
                    "SELECT COUNT(1) FROM seen_events WHERE event_id = @Key",
                    new { Key = seenKey }) > 0;

                if (alreadySeen)
                {
                    _log.LogDebug("[Promoter] Duplicate (seen) item_id={ItemId} sha={Sha} subject={Subject}", itemId, sha, msg.Subject);
                    await msg.AckAsync();
                    continue;
                }

                await db.ExecuteAsync("INSERT INTO seen_events (event_id) VALUES (@Key)", new { Key = seenKey });

                // ---- 6) Versioning via content hash ----
                // Hash exact bytes for now (byte-for-byte change == new version)
                if (msg.Data is null || msg.Data.Length == 0)
                {
                    _log.LogWarning("Empty payload for subject {Subject}; acknowledging without promotion", msg.Subject);
                    await msg.AckAsync();
                    continue;
                }
                var contentHash = Convert.ToBase64String(
                    System.Security.Cryptography.SHA256.HashData(msg.Data));

                var existing = await db.QuerySingleOrDefaultAsync<(int Version, string Hash)>(
                    "SELECT version, content_hash FROM promoter_versions WHERE item_id = @ItemId",
                    new { ItemId = itemId });

                var baseVersion = existing == default ? 0 : existing.Version;
                int newVersion;
                var isSame = existing != default && existing.Hash == contentHash;
                if (existing == default) newVersion = 1;
                else if (isSame)
                {
                    _log.LogInformation("[Promoter] No content change item_id={ItemId} sha={Sha} version={Version} -> ACK skip delta", itemId, sha, existing.Version);
                    // No change → ACK and skip delta
                    await msg.AckAsync();
                    continue;
                }
                else newVersion = existing.Version + 1;

                _log.LogDebug("[Promoter] Versioning item_id={ItemId} sha={Sha} baseVersion={Base} newVersion={New} isSame={IsSame}", itemId, sha, baseVersion, newVersion, isSame);

                // Store current state
                var canonicalJson = Encoding.UTF8.GetString(msg.Data);
                const string upsert = """
                    INSERT INTO promoter_versions (item_id, version, content_hash, canonical_json, last_sha, updated_at)
                    VALUES (@ItemId, @Version, @Hash, @Json, @Sha, CURRENT_TIMESTAMP)
                    ON CONFLICT(item_id) DO UPDATE SET
                        version        = excluded.version,
                        content_hash   = excluded.content_hash,
                        canonical_json = excluded.canonical_json,
                        last_sha       = excluded.last_sha,
                        updated_at     = CURRENT_TIMESTAMP;
                    """;
                await db.ExecuteAsync(upsert, new
                {
                    ItemId = itemId,
                    Version = newVersion,
                    Hash = contentHash,
                    Json = canonicalJson,
                    Sha = sha
                });

                // ---- 7) Emit DELTA (type & subject depend on policy Action) ----
                var deltaType = decision.Action == ActionKind.Retract ? "im.retract.v1" : "im.upsert.v1";
                // DELTAS stream is configured for 'delta.>' subjects
                var deltaSubject = decision.Action == ActionKind.Retract
                    ? $"delta.{org}.{repo}.im.retract"
                    : $"delta.{org}.{repo}.im.upsert";

                // Use event time from payload if present, else now
                var occurredAt = TryGetDateTimeOffset(root, "emitted_at") ?? DateTimeOffset.UtcNow;

                var delta = new
                {
                    type = deltaType,
                    ns = root.GetProperty("ns").GetString(),
                    item_id = itemId,
                    base_version = baseVersion,
                    new_version = newVersion,
                    occurred_at = occurredAt,
                    // pass through useful payload for downstream (or map tighter later)
                    evidence = new { sha, ci = root.GetProperty("ci").GetString() },
                    input_event_id = $"{sha}-{itemId}",
                    policy_version = Environment.GetEnvironmentVariable("PROMOTER_POLICY_VERSION") ?? "v1",
                    source = root.GetProperty("source"),
                    emitted_at = DateTimeOffset.UtcNow.ToString("o")

                };
                var deltaBody = JsonSerializer.SerializeToUtf8Bytes(delta);
                var deltaHeaders = new NatsHeaders
                {
                    { "Nats-Msg-Id", $"delta-{org}-{repo}-{itemId}-{newVersion}-{deltaType}" },
                    { "Content-Type", "application/json" }
                };

                var ack2 = await _js.PublishAsync(deltaSubject, deltaBody, headers: deltaHeaders, cancellationToken: stoppingToken);
                _log.LogDebug("[Promoter] Delta published subject={DeltaSubject} bytes={Bytes} type={Type}", deltaSubject, deltaBody.Length, deltaType);

                _log.LogInformation("Promoted {Action} item={ItemId} v{Version} → {DeltaSubject} (stream={Stream}, seq={Seq})",
                    decision.Action, itemId, newVersion, deltaSubject, ack2.Stream, ack2.Seq);

                // ---- 8) ACK source ----
                await msg.AckAsync();
            }
            catch (OperationCanceledException)
            {
                break; // shutdown
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Error promoting Subject={Subject}", msg.Subject);
                await msg.NakAsync(); // or TermAsync after N retries
            }
        }
    }

    // ---------- Policy ----------
    enum PromotionKind { Promote, Skip, Defer }
    enum ActionKind    { Upsert, Retract }

    readonly record struct PolicyDecision(PromotionKind Kind, ActionKind Action, string? Reason = null);

    static PolicyDecision EvaluatePolicy(JsonElement root)
    {
        // Action: explicit 'action' or trailers.TeamAI-Action
        var actionText = root.TryGetProperty("action", out var actEl) ? actEl.GetString() : null;
        if (string.IsNullOrWhiteSpace(actionText) &&
            root.TryGetProperty("trailers", out var trailers) &&
            trailers.ValueKind == JsonValueKind.Object &&
            trailers.TryGetProperty("TeamAI-Action", out var tAction))
        {
            actionText = tAction.GetString();
        }
        var action = string.Equals(actionText, "retract", StringComparison.OrdinalIgnoreCase)
            ? ActionKind.Retract
            : ActionKind.Upsert;

        // Branch
        var isMain = false;
        if (root.TryGetProperty("source", out var src) && src.TryGetProperty("ref", out var refEl))
        {
            var r = refEl.GetString();
            if (!string.IsNullOrWhiteSpace(r))
            {
                // Accept either full refs/heads/main or bare 'main'
                if (r.EndsWith("/main", StringComparison.OrdinalIgnoreCase) || r.Equals("main", StringComparison.OrdinalIgnoreCase))
                    isMain = true;
                else if (r.EndsWith("/master", StringComparison.OrdinalIgnoreCase) || r.Equals("master", StringComparison.OrdinalIgnoreCase))
                    isMain = true;
            }
        }

        // CI status
        var ci = root.TryGetProperty("ci", out var ciEl) ? ciEl.GetString() : "n/a";

        // Experimental label?
        var experimental = HasLabel(root, "experimental");

        // Example policy:
        // - only main/master
        // - CI must be "green" or "n/a"
        // - skip experimental
        if (!isMain) return new(PromotionKind.Skip, action, "non-main branch");
        if (experimental) return new(PromotionKind.Skip, action, "experimental content");
        if (!string.Equals(ci, "green", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(ci, "n/a", StringComparison.OrdinalIgnoreCase))
            return new(PromotionKind.Skip, action, $"ci={ci}");

        return new(PromotionKind.Promote, action);
    }

    static bool HasLabel(JsonElement root, string label)
    {
        if (root.TryGetProperty("labels", out var labelsEl) && labelsEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var l in labelsEl.EnumerateArray())
                if (string.Equals(l.GetString(), label, StringComparison.OrdinalIgnoreCase))
                    return true;
        }
        return false;
    }

    static DateTimeOffset? TryGetDateTimeOffset(JsonElement root, string name)
    {
        if (root.TryGetProperty(name, out var el))
        {
            if (el.ValueKind == JsonValueKind.String &&
                DateTimeOffset.TryParse(el.GetString(), out var dto))
                return dto;
        }
        return null;
    }
}
