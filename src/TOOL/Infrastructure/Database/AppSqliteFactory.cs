using Dapper;
using Microsoft.Data.Sqlite;

namespace TOOL.Infrastructure.Database;

// Renamed to avoid ambiguity with Microsoft.Data.Sqlite.SqliteFactory
public class AppSqliteFactory
{
    public virtual SqliteConnection Open(string dbPath)
    {
        var db = new SqliteConnection($"Data Source={dbPath}");
        db.Open();

        db.Execute(
            """
                PRAGMA journal_mode = WAL;
                PRAGMA busy_timeout = 3000;
            """
        );

        return db;
    }
}
