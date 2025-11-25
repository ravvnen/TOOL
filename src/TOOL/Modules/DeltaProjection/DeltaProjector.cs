using Dapper;
using Microsoft.Data.Sqlite;

namespace Modules.DeltaProjection;

/// <summary>
/// Handles projection of DELTA events to SQLite database.
/// Maintains im_items_current, im_items_history, and source_bindings tables.
/// </summary>
public sealed class DeltaProjector
{
    /// <summary>
    /// Apply a delta event to the projection database.
    /// Handles both upserts (create/update) and retracts (delete).
    /// </summary>
    public async Task ApplyAsync(
        SqliteConnection db,
        DeltaEvent delta,
        CancellationToken ct = default
    )
    {
        using var tx = db.BeginTransaction();

        // Check idempotency
        var alreadySeen =
            await db.ExecuteScalarAsync<long>(
                "SELECT COUNT(1) FROM deltas_seen_events WHERE ns=@Ns AND event_id=@Eid",
                new { Ns = delta.Ns, Eid = delta.InputEventId }
            ) > 0;

        if (alreadySeen)
        {
            tx.Commit();
            return; // Already processed
        }

        // Apply delta based on type
        if (delta.IsUpsert)
        {
            await ApplyUpsertAsync(db, delta, tx);
        }
        else if (delta.IsRetract)
        {
            await ApplyRetractAsync(db, delta, tx);
        }

        // Mark as seen
        await db.ExecuteAsync(
            "INSERT OR IGNORE INTO deltas_seen_events(ns,event_id) VALUES(@Ns,@Eid)",
            new { Ns = delta.Ns, Eid = delta.InputEventId },
            tx
        );

        tx.Commit();
    }

    /// <summary>
    /// Initialize database schema (tables for projection).
    /// </summary>
    public async Task InitializeSchemaAsync(SqliteConnection db, CancellationToken ct = default)
    {
        await db.ExecuteAsync(
            @"
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

            CREATE TABLE IF NOT EXISTS deltas_seen_events (
                ns TEXT NOT NULL,
                event_id TEXT NOT NULL,
                PRIMARY KEY (ns, event_id)
            );
            "
        );
    }

    // ===== Private methods =====

    private async Task ApplyUpsertAsync(SqliteConnection db, DeltaEvent delta, SqliteTransaction tx)
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
                Ns = delta.Ns,
                ItemId = delta.ItemId,
                Version = delta.NewVersion,
                Title = delta.Title,
                Content = delta.Content,
                Labels = delta.LabelsJson,
                Policy = delta.PolicyVersion,
                OccurredAt = delta.OccurredAt.ToString("o"),
                EmittedAt = delta.EmittedAt.ToString("o"),
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
                Ns = delta.Ns,
                ItemId = delta.ItemId,
                Version = delta.NewVersion,
                Title = delta.Title,
                Content = delta.Content,
                Labels = delta.LabelsJson,
                Policy = delta.PolicyVersion,
                OccurredAt = delta.OccurredAt.ToString("o"),
                EmittedAt = delta.EmittedAt.ToString("o"),
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
                Ns = delta.Ns,
                ItemId = delta.ItemId,
                Version = delta.NewVersion,
                Repo = delta.Repo,
                Ref = delta.Ref,
                Path = delta.Path,
                BlobSha = delta.BlobSha,
            },
            tx
        );
    }

    private async Task ApplyRetractAsync(
        SqliteConnection db,
        DeltaEvent delta,
        SqliteTransaction tx
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
                Ns = delta.Ns,
                ItemId = delta.ItemId,
                Version = delta.NewVersion,
                Policy = delta.PolicyVersion,
                OccurredAt = delta.OccurredAt.ToString("o"),
                EmittedAt = delta.EmittedAt.ToString("o"),
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
                Ns = delta.Ns,
                ItemId = delta.ItemId,
                Version = delta.NewVersion,
                Title = delta.Title,
                Content = delta.Content,
                Labels = delta.LabelsJson,
                Policy = delta.PolicyVersion,
                OccurredAt = delta.OccurredAt.ToString("o"),
                EmittedAt = delta.EmittedAt.ToString("o"),
            },
            tx
        );

        // Source binding (preserve provenance chain for retractions)
        await db.ExecuteAsync(
            @"
            INSERT OR REPLACE INTO source_bindings
              (ns,item_id,version,repo,ref,path,blob_sha)
            VALUES
              (@Ns,@ItemId,@Version,@Repo,@Ref,@Path,@BlobSha)",
            new
            {
                Ns = delta.Ns,
                ItemId = delta.ItemId,
                Version = delta.NewVersion,
                Repo = delta.Repo,
                Ref = delta.Ref,
                Path = delta.Path,
                BlobSha = delta.BlobSha,
            },
            tx
        );
    }
}
