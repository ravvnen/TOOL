using Dapper;
using Microsoft.Data.Sqlite;

namespace TOOL.Services;

// Renamed to avoid ambiguity with Microsoft.Data.Sqlite.SqliteFactory
public class AppSqliteFactory
{
    public virtual SqliteConnection Open(string path)
    {
        var db = new SqliteConnection($"Data Source={path}");
        db.Open();

        db.Execute(
            """
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
            """
        );

        // Optional FTS5 to improve search
        db.Execute(
            """
                CREATE VIRTUAL TABLE IF NOT EXISTS im_fts USING fts5(
                    ns UNINDEXED,
                    item_id UNINDEXED,
                    title,
                    content,
                    tokenize = 'porter'
                );
            """
        );

        db.Execute(
            """
                CREATE TRIGGER IF NOT EXISTS im_fts_up AFTER INSERT ON im_items_current BEGIN
                    INSERT INTO im_fts(ns,item_id,title,content) VALUES (new.ns,new.item_id,new.title,new.content);
                END;
            """
        );

        db.Execute(
            """
                CREATE TRIGGER IF NOT EXISTS im_fts_upd AFTER UPDATE ON im_items_current BEGIN
                    DELETE FROM im_fts WHERE ns=old.ns AND item_id=old.item_id;
                    INSERT INTO im_fts(ns,item_id,title,content) VALUES (new.ns,new.item_id,new.title,new.content);
                END;
            """
        );

        db.Execute(
            """
                CREATE TRIGGER IF NOT EXISTS im_fts_del AFTER DELETE ON im_items_current BEGIN
                    DELETE FROM im_fts WHERE ns=old.ns AND item_id=old.item_id;
                END;
            """
        );

        return db;
    }
}
