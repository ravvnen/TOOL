using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Dapper;
using Microsoft.Data.Sqlite;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;

// NOTE: This promoter listens to evt.> (all events). If you only want proposals, use "evt.>.proposal.v1".
public sealed class Promoter : BackgroundService
{
    private readonly NatsJSContext _js;
    private readonly ILogger<Promoter> _log;
    private readonly string _dbPath;
    private readonly string _durable;
    private readonly string _policyVersion;

    public Promoter(NatsJSContext js, ILogger<Promoter> log)
    {
        _js = js;
        _log = log;
        _dbPath = Environment.GetEnvironmentVariable("PROMOTER_DB_PATH") ?? "promoter.db";
        _durable = Environment.GetEnvironmentVariable("PROMOTER_DURABLE") ?? "promoter-main";
        _policyVersion =
            Environment.GetEnvironmentVariable("PROMOTER_POLICY_VERSION") ?? "promoter-1.0.0";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // 0) Ensure AUDIT stream exists (emit-only; consumers optional)
        await EnsureAuditStreamAsync(stoppingToken);

        // 1) DB init (WAL, indices, normalized schema)
        await using var db = new SqliteConnection($"Data Source={_dbPath}");
        await db.OpenAsync(stoppingToken);

        const string ddl = """
            PRAGMA journal_mode = WAL;
            PRAGMA busy_timeout = 3000;

            CREATE TABLE IF NOT EXISTS promoter_items (
                ns              TEXT NOT NULL,
                item_id         TEXT NOT NULL,
                version         INTEGER NOT NULL,
                title           TEXT NOT NULL,
                content         TEXT NOT NULL,
                labels_json     TEXT NOT NULL,
                content_hash    TEXT NOT NULL,
                is_active       INTEGER NOT NULL,
                policy_version  TEXT NOT NULL,
                source_repo     TEXT NOT NULL,
                source_ref      TEXT NOT NULL,
                source_path     TEXT NOT NULL,
                source_blob_sha TEXT NOT NULL,
                updated_at      TEXT NOT NULL,
                PRIMARY KEY (ns, item_id)
            );

            CREATE UNIQUE INDEX IF NOT EXISTS ux_items_ns_hash
              ON promoter_items(ns, content_hash);

            CREATE TABLE IF NOT EXISTS promoter_item_versions (
                ns              TEXT NOT NULL,
                item_id         TEXT NOT NULL,
                version         INTEGER NOT NULL,
                title           TEXT NOT NULL,
                content         TEXT NOT NULL,
                labels_json     TEXT NOT NULL,
                content_hash    TEXT NOT NULL,
                input_event_id  TEXT NOT NULL,
                policy_version  TEXT NOT NULL,
                source_repo     TEXT NOT NULL,
                source_ref      TEXT NOT NULL,
                source_path     TEXT NOT NULL,
                source_blob_sha TEXT NOT NULL,
                emitted_at      TEXT NOT NULL,
                PRIMARY KEY (ns, item_id, version)
            );

            CREATE TABLE IF NOT EXISTS promoter_seen_events (
                ns       TEXT NOT NULL,
                event_id TEXT NOT NULL,
                PRIMARY KEY (ns, event_id)
            );

            -- Compact audit projection for forensics/metrics
            CREATE TABLE IF NOT EXISTS promoter_audit (
                ns             TEXT NOT NULL,
                decision_id    TEXT PRIMARY KEY,
                item_id        TEXT NOT NULL,
                sha            TEXT NOT NULL,
                action         TEXT NOT NULL,         -- upsert|retract|skip|defer
                reason_code    TEXT NOT NULL,
                reason_detail  TEXT,
                policy_version TEXT NOT NULL,
                input_subject  TEXT NOT NULL,
                input_hash     TEXT NOT NULL,
                prior_version  INTEGER,
                prior_hash     TEXT,
                new_version    INTEGER,
                is_same_hash   INTEGER NOT NULL,
                delta_type     TEXT,
                delta_subject  TEXT,
                delta_msg_id   TEXT,
                deltas_stream  TEXT,
                deltas_seq     INTEGER,
                received_at    TEXT NOT NULL,
                decided_at     TEXT NOT NULL,
                published_at   TEXT,
                latency_ms     INTEGER,
                emitted_at     TEXT NOT NULL
            );

            CREATE INDEX IF NOT EXISTS ix_promoter_audit_ns     ON promoter_audit(ns);
            CREATE INDEX IF NOT EXISTS ix_promoter_audit_item   ON promoter_audit(ns, item_id);
            CREATE INDEX IF NOT EXISTS ix_promoter_audit_action ON promoter_audit(ns, action);
            CREATE INDEX IF NOT EXISTS ix_promoter_audit_reason ON promoter_audit(ns, reason_code);
            """;
        await db.ExecuteAsync(ddl);

        // 2) Durable consumer on EVENTS (you can narrow to "evt.>.proposal.v1" if desired)
        var filter = "evt.>";
        var consumer = await _js.CreateOrUpdateConsumerAsync(
            "EVENTS",
            new ConsumerConfig
            {
                DurableName = _durable,
                Name = _durable,
                FilterSubject = filter,
                DeliverPolicy = ConsumerConfigDeliverPolicy.All,
                AckPolicy = ConsumerConfigAckPolicy.Explicit,
                MaxAckPending = 256,
            },
            stoppingToken
        );

        _log.LogInformation(
            "Promoter started: Stream=EVENTS Filter={Filter} Durable={Durable}",
            filter,
            _durable
        );
        _log.LogDebug("[Promoter] DB path {DbPath}", _dbPath);

        // 3) Consume proposals
        await foreach (var msg in consumer.ConsumeAsync<byte[]>().WithCancellation(stoppingToken))
        {
            var swStart = Stopwatch.GetTimestamp();
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

                // -------- Extract required fields (strict, with logging) --------
                if (!TryGetString(root, "ns", out var ns) || string.IsNullOrWhiteSpace(ns))
                {
                    _log.LogWarning("Missing ns. Subject={Subject}", msg.Subject);
                    await msg.NakAsync();
                    continue;
                }
                if (
                    !TryGetString(root, "item_id", out var itemId)
                    || string.IsNullOrWhiteSpace(itemId)
                )
                {
                    _log.LogWarning("Missing item_id. Subject={Subject}", msg.Subject);
                    await msg.NakAsync();
                    continue;
                }
                if (!TryGetString(root, "sha", out var sha) || string.IsNullOrWhiteSpace(sha))
                {
                    _log.LogWarning("Missing sha. Subject={Subject}", msg.Subject);
                    await msg.NakAsync();
                    continue;
                }

                var titleRaw = GetStringOrEmpty(root, "title");
                var contentRaw = GetStringOrEmpty(root, "content");
                var labels = GetStringArray(root, "labels");

                if (
                    !root.TryGetProperty("source", out var source)
                    || source.ValueKind != JsonValueKind.Object
                )
                {
                    _log.LogWarning("Missing source. Subject={Subject}", msg.Subject);
                    await msg.NakAsync();
                    continue;
                }

                var repo = GetStringOrEmpty(source, "repo");
                var srcRef = GetStringOrEmpty(source, "ref");
                var path = GetStringOrEmpty(source, "path");
                var blobSha = GetStringOrEmpty(source, "blob_sha");

                // -------- Gate policy (branch, ci, labels, retract/ upsert) --------
                var decision = EvaluatePolicy(root);

                // -------- Canonicalization & hash (CONTENT-BASED) --------
                var canonTitle = Canonicalize(titleRaw);
                var canonContent = Canonicalize(contentRaw);
                var labelsJson = JsonSerializer.Serialize(labels);
                var contentHash = HexSha256($"{itemId}\n{canonTitle}\n{canonContent}");

                // Idempotency (mark seen only after successful publish)
                var seenKey = $"{sha}-{itemId}";
                var alreadySeen =
                    await db.ExecuteScalarAsync<long>(
                        "SELECT COUNT(1) FROM promoter_seen_events WHERE ns=@Ns AND event_id=@Eid",
                        new { Ns = ns, Eid = seenKey }
                    ) > 0;

                // Current state
                var prior = await db.QuerySingleOrDefaultAsync<(
                    int Version,
                    int Active,
                    string Hash
                )>(
                    "SELECT version as Version, is_active as Active, content_hash as Hash "
                        + "FROM promoter_items WHERE ns=@Ns AND item_id=@ItemId",
                    new { Ns = ns, ItemId = itemId }
                );

                var baseVersion = prior == default ? 0 : prior.Version;
                var isSameActive =
                    prior != default && prior.Active == 1 && prior.Hash == contentHash;

                // Decide action/reason up front for audit (but we still may early-return)
                var actionStr = decision.Kind switch
                {
                    PromotionKind.Skip => "skip",
                    PromotionKind.Defer => "defer",
                    _ when decision.Action == ActionKind.Retract => "retract",
                    _ => "upsert",
                };

                string reasonCode =
                    decision.Kind == PromotionKind.Skip ? MapSkipReasonToCode(decision.Reason, root)
                    : decision.Kind == PromotionKind.Defer ? "defer:transient"
                    : (isSameActive ? "unchanged" : "ok");

                // Handle Skip/Defer early, but write AUDIT for them too
                if (decision.Kind == PromotionKind.Skip || decision.Kind == PromotionKind.Defer)
                {
                    await EmitAuditAsync(
                        db,
                        msg,
                        ns,
                        itemId,
                        sha,
                        actionStr,
                        reasonCode,
                        decision.Reason,
                        contentHash,
                        baseVersion,
                        prior == default ? null : prior.Hash,
                        newVersion: null,
                        isSameHash: isSameActive,
                        deltaType: null,
                        deltaSubject: null,
                        deltaMsgId: null,
                        deltasStream: null,
                        deltasSeq: null,
                        swStart,
                        receivedAt: TryGetDateTimeOffset(root, "emitted_at")
                            ?? DateTimeOffset.UtcNow
                    );

                    if (decision.Kind == PromotionKind.Skip)
                    {
                        _log.LogInformation(
                            "Skip: {Reason} (ns={Ns}, item_id={ItemId}, sha={Sha})",
                            decision.Reason,
                            ns,
                            itemId,
                            sha
                        );
                        await msg.AckAsync();
                    }
                    else
                    {
                        _log.LogInformation(
                            "Defer: {Reason} (ns={Ns}, item_id={ItemId}, sha={Sha})",
                            decision.Reason,
                            ns,
                            itemId,
                            sha
                        );
                        await msg.NakAsync(); // transient — try again later
                    }
                    continue;
                }

                // At this point: Promote (Upsert or Retract)
                int newVersion;
                bool isActiveAfter;

                if (decision.Action == ActionKind.Retract)
                {
                    newVersion = prior == default ? 1 : baseVersion + 1;
                    isActiveAfter = false;
                }
                else
                {
                    if (alreadySeen || isSameActive)
                    {
                        // No-op: no change in active content OR duplicate event fully processed earlier
                        await EmitAuditAsync(
                            db,
                            msg,
                            ns,
                            itemId,
                            sha,
                            "skip",
                            alreadySeen ? "duplicate" : "unchanged",
                            alreadySeen ? "duplicate (seen_events)" : "No content change",
                            contentHash,
                            baseVersion,
                            prior == default ? null : prior.Hash,
                            newVersion: null,
                            isSameHash: true,
                            deltaType: null,
                            deltaSubject: null,
                            deltaMsgId: null,
                            deltasStream: null,
                            deltasSeq: null,
                            swStart,
                            receivedAt: TryGetDateTimeOffset(root, "emitted_at")
                                ?? DateTimeOffset.UtcNow
                        );

                        if (!alreadySeen)
                        {
                            _log.LogInformation(
                                "No content change (noop). ns={Ns} item={ItemId} v={V} sha={Sha}",
                                ns,
                                itemId,
                                baseVersion,
                                sha
                            );
                            await MarkSeenAsync(db, ns, seenKey); // mark seen for this noop
                        }
                        await msg.AckAsync();
                        continue;
                    }
                    newVersion = prior == default ? 1 : baseVersion + 1;
                    isActiveAfter = true;
                }

                // -------- Promote within a DB transaction --------
                using var tx = db.BeginTransaction();

                // Upsert current snapshot
                await db.ExecuteAsync(
                    @"
                    INSERT INTO promoter_items
                      (ns,item_id,version,title,content,labels_json,content_hash,is_active,policy_version,
                       source_repo,source_ref,source_path,source_blob_sha,updated_at)
                    VALUES
                      (@Ns,@ItemId,@Version,@Title,@Content,@Labels,@Hash,@Active,@Policy,
                       @Repo,@Ref,@Path,@BlobSha,CURRENT_TIMESTAMP)
                    ON CONFLICT(ns,item_id) DO UPDATE SET
                      version=excluded.version,
                      title=excluded.title,
                      content=excluded.content,
                      labels_json=excluded.labels_json,
                      content_hash=excluded.content_hash,
                      is_active=excluded.is_active,
                      policy_version=excluded.policy_version,
                      source_repo=excluded.source_repo,
                      source_ref=excluded.source_ref,
                      source_path=excluded.source_path,
                      source_blob_sha=excluded.source_blob_sha,
                      updated_at=CURRENT_TIMESTAMP",
                    new
                    {
                        Ns = ns,
                        ItemId = itemId,
                        Version = newVersion,
                        Title = canonTitle,
                        Content = canonContent,
                        Labels = labelsJson,
                        Hash = contentHash,
                        Active = isActiveAfter ? 1 : 0,
                        Policy = _policyVersion,
                        Repo = repo,
                        Ref = srcRef,
                        Path = path,
                        BlobSha = blobSha,
                    },
                    tx
                );

                // Append version history
                var inputEventId = $"{sha}-{itemId}";
                await db.ExecuteAsync(
                    @"
                    INSERT INTO promoter_item_versions
                      (ns,item_id,version,title,content,labels_json,content_hash,input_event_id,policy_version,
                       source_repo,source_ref,source_path,source_blob_sha,emitted_at)
                    VALUES
                      (@Ns,@ItemId,@Version,@Title,@Content,@Labels,@Hash,@InputId,@Policy,
                       @Repo,@Ref,@Path,@BlobSha,@EmittedAt)",
                    new
                    {
                        Ns = ns,
                        ItemId = itemId,
                        Version = newVersion,
                        Title = canonTitle,
                        Content = canonContent,
                        Labels = labelsJson,
                        Hash = contentHash,
                        InputId = inputEventId,
                        Policy = _policyVersion,
                        Repo = repo,
                        Ref = srcRef,
                        Path = path,
                        BlobSha = blobSha,
                        EmittedAt = DateTimeOffset.UtcNow.ToString("o"),
                    },
                    tx
                );

                tx.Commit();

                // -------- Emit DELTA (idempotent publish via Nats-Msg-Id) --------
                var deltaType = isActiveAfter ? "im.upsert.v1" : "im.retract.v1";
                var deltaSubject = $"delta.{ns}.im.{(isActiveAfter ? "upsert" : "retract")}.v1";
                var occurredAt = TryGetDateTimeOffset(root, "emitted_at") ?? DateTimeOffset.UtcNow;

                byte[] deltaBody = isActiveAfter
                    ? JsonSerializer.SerializeToUtf8Bytes(
                        new
                        {
                            type = "im.upsert.v1",
                            ns,
                            item_id = itemId,
                            base_version = baseVersion,
                            new_version = newVersion,
                            title = canonTitle,
                            content = canonContent,
                            labels = labels,
                            input_event_id = inputEventId,
                            input_hash = contentHash,
                            policy_version = _policyVersion,
                            source = new
                            {
                                repo,
                                @ref = srcRef,
                                path,
                                blob_sha = blobSha,
                            },
                            occurred_at = occurredAt,
                            emitted_at = DateTimeOffset.UtcNow.ToString("o"),
                        }
                    )
                    : JsonSerializer.SerializeToUtf8Bytes(
                        new
                        {
                            type = "im.retract.v1",
                            ns,
                            item_id = itemId,
                            base_version = baseVersion,
                            new_version = newVersion,
                            input_event_id = inputEventId,
                            policy_version = _policyVersion,
                            source = new
                            {
                                repo,
                                @ref = srcRef,
                                path,
                                blob_sha = blobSha,
                            },
                            occurred_at = occurredAt,
                            emitted_at = DateTimeOffset.UtcNow.ToString("o"),
                        }
                    );

                var msgId = $"delta:{ns}:{itemId}:v{newVersion}:{deltaType}";
                var headers = new NatsHeaders
                {
                    { "Nats-Msg-Id", msgId },
                    { "Content-Type", "application/json" },
                };

                var ack = await _js.PublishAsync(
                    deltaSubject,
                    deltaBody,
                    headers: headers,
                    cancellationToken: stoppingToken
                );
                _log.LogInformation(
                    "Promoted {Type} ns={Ns} item={Item} v{V} → {Subj} (stream={Stream}, seq={Seq})",
                    deltaType,
                    ns,
                    itemId,
                    newVersion,
                    deltaSubject,
                    ack.Stream,
                    ack.Seq
                );

                // ---- AUDIT for the promotion ----
                await EmitAuditAsync(
                    db,
                    msg,
                    ns,
                    itemId,
                    sha,
                    actionStr: decision.Action == ActionKind.Retract ? "retract" : "upsert",
                    reasonCode: isSameActive ? "unchanged" : "ok",
                    reasonDetail: null,
                    contentHash: contentHash,
                    priorVersion: baseVersion,
                    priorHash: prior == default ? null : prior.Hash,
                    newVersion: newVersion,
                    isSameHash: isSameActive,
                    deltaType: deltaType,
                    deltaSubject: deltaSubject,
                    deltaMsgId: msgId,
                    deltasStream: ack.Stream,
                    deltasSeq: (int)ack.Seq,
                    swStart,
                    receivedAt: occurredAt
                );

                // Mark seen AFTER successful publish
                await MarkSeenAsync(db, ns, seenKey);

                // ACK source
                await msg.AckAsync();
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Error promoting Subject={Subject}", msg.Subject);
                try
                {
                    await msg.NakAsync();
                }
                catch
                { /* ignore */
                }
            }
        }
    }

    // ===================== Helpers & Policy =====================

    enum PromotionKind
    {
        Promote,
        Skip,
        Defer,
    }

    enum ActionKind
    {
        Upsert,
        Retract,
    }

    readonly record struct PolicyDecision(
        PromotionKind Kind,
        ActionKind Action,
        string? Reason = null
    );

    static PolicyDecision EvaluatePolicy(JsonElement root)
    {
        // Action: explicit 'action' or trailers.TeamAI-Action
        var actionText = TryGetString(root, "action", out var act) ? act : null;
        if (
            string.IsNullOrWhiteSpace(actionText)
            && root.TryGetProperty("trailers", out var trailers)
            && trailers.ValueKind == JsonValueKind.Object
            && trailers.TryGetProperty("TeamAI-Action", out var tAction)
        )
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
                if (
                    r.EndsWith("/main", StringComparison.OrdinalIgnoreCase)
                    || r.Equals("main", StringComparison.OrdinalIgnoreCase)
                )
                    isMain = true;
                else if (
                    r.EndsWith("/master", StringComparison.OrdinalIgnoreCase)
                    || r.Equals("master", StringComparison.OrdinalIgnoreCase)
                )
                    isMain = true;
            }
        }

        // CI status
        var ci = TryGetString(root, "ci", out var ciVal) ? ciVal : "n/a";

        // Experimental label?
        var experimental = HasLabel(root, "experimental");

        // Gate:
        if (!isMain)
            return new(PromotionKind.Skip, action, "non-main branch");
        if (experimental)
            return new(PromotionKind.Skip, action, "experimental content");
        if (
            !string.Equals(ci, "green", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(ci, "n/a", StringComparison.OrdinalIgnoreCase)
        )
            return new(PromotionKind.Skip, action, $"ci={ci}");

        return new(PromotionKind.Promote, action);
    }

    static string MapSkipReasonToCode(string? reason, JsonElement root)
    {
        if (string.Equals(reason, "non-main branch", StringComparison.OrdinalIgnoreCase))
            return "branch:not-main";
        if (string.Equals(reason, "experimental content", StringComparison.OrdinalIgnoreCase))
            return "experimental";

        if (root.TryGetProperty("ci", out var ciEl))
        {
            var ci = ciEl.GetString() ?? "";
            if (
                !string.Equals(ci, "green", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(ci, "n/a", StringComparison.OrdinalIgnoreCase)
            )
                return "ci:not-green";
        }
        return "skip:other";
    }

    static bool HasLabel(JsonElement root, string label)
    {
        if (
            root.TryGetProperty("labels", out var labelsEl)
            && labelsEl.ValueKind == JsonValueKind.Array
        )
        {
            foreach (var l in labelsEl.EnumerateArray())
                if (string.Equals(l.GetString(), label, StringComparison.OrdinalIgnoreCase))
                    return true;
        }
        return false;
    }

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

    static string[] GetStringArray(JsonElement el, string name)
    {
        if (el.TryGetProperty(name, out var arr) && arr.ValueKind == JsonValueKind.Array)
            return arr.EnumerateArray()
                .Select(x => x.GetString())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Cast<string>()
                .ToArray();
        return Array.Empty<string>();
    }

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

    static string Canonicalize(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return "";
        var t = s.Replace("\r\n", "\n").Trim();
        t = Regex.Replace(t, @"[ \t]+", " "); // collapse runs of spaces/tabs
        return t;
    }

    static string HexSha256(string s)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(s)));
    }

    static async Task MarkSeenAsync(SqliteConnection db, string ns, string seenKey)
    {
        await db.ExecuteAsync(
            "INSERT OR IGNORE INTO promoter_seen_events(ns,event_id) VALUES(@Ns,@Eid)",
            new { Ns = ns, Eid = seenKey }
        );
    }

    private async Task EnsureAuditStreamAsync(CancellationToken ct)
    {
        try
        {
            // Idempotent stream create/update
            await _js.CreateOrUpdateStreamAsync(
                new StreamConfig
                {
                    Name = "AUDITS",
                    Subjects = new[] { "audit.>" },
                    Storage = StreamConfigStorage.File, // File or Memory
                    Retention = StreamConfigRetention.Limits, // default size/count retention; tune if needed
                    // MaxAge can be added if you want TTL; omitted to keep all decisions by default
                },
                ct
            );
            _log.LogInformation("AUDIT stream ensured (audit.>)");
        }
        catch (Exception ex)
        {
            _log.LogWarning(
                ex,
                "Failed to ensure AUDIT stream; continuing (audit emits may fail later)"
            );
        }
    }

    private async Task EmitAuditAsync(
        SqliteConnection db,
        INatsJSMsg<byte[]> msg,
        string ns,
        string itemId,
        string sha,
        string actionStr,
        string reasonCode,
        string? reasonDetail,
        string contentHash,
        int priorVersion,
        string? priorHash,
        int? newVersion,
        bool isSameHash,
        string? deltaType,
        string? deltaSubject,
        string? deltaMsgId,
        string? deltasStream,
        int? deltasSeq,
        long swStart,
        DateTimeOffset receivedAt
    )
    {
        var decidedAt = DateTimeOffset.UtcNow;
        var latencyMs = (int)((Stopwatch.GetTimestamp() - swStart) * 1000.0 / Stopwatch.Frequency);
        var decisionId = Guid.NewGuid().ToString("N");

        var audit = new
        {
            type = "promoter.decision.v1",
            ns,
            decision_id = decisionId,
            action = actionStr,
            reason_code = reasonCode,
            reason_detail = reasonDetail,
            policy_version = _policyVersion,
            input = new
            {
                subject = msg.Subject,
                item_id = itemId,
                sha,
                content_hash = contentHash,
                ci = ExtractString(msg, "ci"),
                branch_ref = ExtractSourceRef(msg),
                labels = ExtractLabels(msg),
            },
            state = new
            {
                prior_version = priorVersion,
                prior_hash = priorHash,
                new_version = newVersion,
                is_same_hash = isSameHash,
            },
            output = new
            {
                emitted = deltaType is not null,
                delta_type = deltaType,
                delta_subject = deltaSubject,
                delta_msg_id = deltaMsgId,
                stream = deltasStream,
                stream_seq = deltasSeq,
            },
            timing = new
            {
                received_at = receivedAt,
                decided_at = decidedAt,
                published_at = deltaType is not null ? decidedAt : (DateTimeOffset?)null,
                latency_ms = latencyMs,
            },
            emitted_at = DateTimeOffset.UtcNow,
        };

        // Publish to AUDIT stream
        var auditSubject = $"audit.{ns}.promoter.decision.v1";
        var auditBody = JsonSerializer.SerializeToUtf8Bytes(audit);
        var auditHeaders = new NatsHeaders
        {
            { "Nats-Msg-Id", $"audit:{ns}:{decisionId}" },
            { "Content-Type", "application/json" },
        };

        try
        {
            await _js.PublishAsync(auditSubject, auditBody, headers: auditHeaders);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Failed to publish audit event (non-fatal)");
        }

        // Best-effort SQL insert (does not affect control flow)
        try
        {
            await db.ExecuteAsync(
                @"
                INSERT INTO promoter_audit
                  (ns,decision_id,item_id,sha,action,reason_code,reason_detail,policy_version,
                   input_subject,input_hash,prior_version,prior_hash,new_version,is_same_hash,
                   delta_type,delta_subject,delta_msg_id,deltas_stream,deltas_seq,
                   received_at,decided_at,published_at,latency_ms,emitted_at)
                VALUES
                  (@ns,@decision_id,@item_id,@sha,@action,@reason_code,@reason_detail,@policy_version,
                   @input_subject,@input_hash,@prior_version,@prior_hash,@new_version,@is_same_hash,
                   @delta_type,@delta_subject,@delta_msg_id,@deltas_stream,@deltas_seq,
                   @received_at,@decided_at,@published_at,@latency_ms,@emitted_at)",
                new
                {
                    ns,
                    decision_id = decisionId,
                    item_id = itemId,
                    sha,
                    action = actionStr,
                    reason_code = reasonCode,
                    reason_detail = reasonDetail,
                    policy_version = _policyVersion,
                    input_subject = msg.Subject,
                    input_hash = contentHash,
                    prior_version = priorVersion,
                    prior_hash = priorHash,
                    new_version = newVersion,
                    is_same_hash = isSameHash ? 1 : 0,
                    delta_type = deltaType,
                    delta_subject = deltaSubject,
                    delta_msg_id = deltaMsgId,
                    deltas_stream = deltasStream,
                    deltas_seq = deltasSeq,
                    received_at = receivedAt.ToString("o"),
                    decided_at = decidedAt.ToString("o"),
                    published_at = (
                        deltaType is not null ? decidedAt : (DateTimeOffset?)null
                    )?.ToString("o"),
                    latency_ms = latencyMs,
                    emitted_at = DateTimeOffset.UtcNow.ToString("o"),
                }
            );
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Failed to persist promoter_audit row (non-fatal)");
        }
    }

    // Extract helpers for audit (works from the already-parsed payload in handler)
    static string? ExtractString(INatsJSMsg<byte[]> msg, string name)
    {
        try
        {
            using var doc = JsonDocument.Parse(msg.Data!);
            var root = doc.RootElement;
            if (root.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.String)
                return el.GetString();
        }
        catch { }
        return null;
    }

    static string? ExtractSourceRef(INatsJSMsg<byte[]> msg)
    {
        try
        {
            using var doc = JsonDocument.Parse(msg.Data!);
            var root = doc.RootElement;
            if (
                root.TryGetProperty("source", out var src)
                && src.TryGetProperty("ref", out var r)
                && r.ValueKind == JsonValueKind.String
            )
                return r.GetString();
        }
        catch { }
        return null;
    }

    static string[] ExtractLabels(INatsJSMsg<byte[]> msg)
    {
        try
        {
            using var doc = JsonDocument.Parse(msg.Data!);
            var root = doc.RootElement;
            if (root.TryGetProperty("labels", out var arr) && arr.ValueKind == JsonValueKind.Array)
                return arr.EnumerateArray()
                    .Select(x => x.GetString())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Cast<string>()
                    .ToArray();
        }
        catch { }
        return Array.Empty<string>();
    }
}
