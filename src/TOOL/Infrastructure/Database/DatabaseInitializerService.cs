namespace TOOL.Infrastructure.Database;

public sealed class DatabaseInitializerService : IHostedService
{
    private readonly AppSqliteFactory _dbFactory;
    private readonly ILogger<DatabaseInitializerService> _log;

    public DatabaseInitializerService(
        AppSqliteFactory dbFactory,
        ILogger<DatabaseInitializerService> log
    )
    {
        _dbFactory = dbFactory;
        _log = log;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var dbPath = Environment.GetEnvironmentVariable("RULES_DB_PATH") ?? "rules.db";

        try
        {
            _log.LogInformation("Initializing database: {DbPath}", dbPath);

            // Open database connection - this will create the database and all tables/triggers
            await using var db = _dbFactory.Open(dbPath);

            _log.LogInformation("Database initialized successfully: {DbPath}", dbPath);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Failed to initialize database: {DbPath}", dbPath);
            throw; // Fail startup if database can't be initialized
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
