using Dapper;
using Microsoft.Data.Sqlite;

namespace Infrastructure.Database.Schema;

public class RulesDbSchema
{
    public async Task InitializeAsync(SqliteConnection db, CancellationToken cancellationToken)
    {
        await CreateTableAsync(db, cancellationToken);
        await CreateIndexAsync(db, cancellationToken);
        await CreateTriggersAsync(db, cancellationToken);
    }

    private async Task CreateTableAsync(SqliteConnection db, CancellationToken cancellationToken)
    {
        // Create current items table
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
            ",
            cancellationToken
        );

        // Create history items table
        await db.ExecuteAsync(
            @"
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
            ",
            cancellationToken
        );

        // Create source bindings table
        await db.ExecuteAsync(
            @"
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
            ",
            cancellationToken
        );

        // Create deltas seen events table
        // Used for idempotency tracking of processed delta events.
        await db.ExecuteAsync(
            @"
        CREATE TABLE IF NOT EXISTS deltas_seen_events (
                ns TEXT NOT NULL,
                event_id TEXT NOT NULL,
                PRIMARY KEY (ns, event_id)
            );
            ",
            cancellationToken
        );
    }

    private async Task CreateIndexAsync(SqliteConnection db, CancellationToken cancellationToken)
    {
        await db.ExecuteAsync(
            @"
        CREATE VIRTUAL TABLE IF NOT EXISTS im_fts USING fts5(
            ns UNINDEXED,
            item_id UNINDEXED,
            title,
            content,
            tokenize = 'porter'
        );
        "
        );
    }

    private async Task CreateTriggersAsync(SqliteConnection db, CancellationToken cancellationToken)
    {
        // Insert trigger
        await db.ExecuteAsync(
            @"
            CREATE TRIGGER IF NOT EXISTS im_fts_up AFTER INSERT ON im_items_current BEGIN
                INSERT INTO im_fts(ns,item_id,title,content) VALUES (new.ns,new.item_id,new.title,new.content);
            END;
            ",
            cancellationToken
        );

        // Update trigger
        await db.ExecuteAsync(
            @"
            CREATE TRIGGER IF NOT EXISTS im_fts_upd AFTER UPDATE ON im_items_current BEGIN
                DELETE FROM im_fts WHERE ns=old.ns AND item_id=old.item_id;
                INSERT INTO im_fts(ns,item_id,title,content) VALUES (new.ns,new.item_id,new.title,new.content);
            END;
            ",
            cancellationToken
        );

        // Delete trigger
        await db.ExecuteAsync(
            @"
            CREATE TRIGGER IF NOT EXISTS im_fts_del AFTER DELETE ON im_items_current BEGIN
                DELETE FROM im_fts WHERE ns=old.ns AND item_id=old.item_id;
            END;
            ",
            cancellationToken
        );
    }
}
