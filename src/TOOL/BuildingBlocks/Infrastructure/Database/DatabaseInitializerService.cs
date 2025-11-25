using Infrastructure.Database.Schema;

namespace Infrastructure.Database;

public sealed class DatabaseInitializerService : IHostedService
{
    private readonly AppSqliteFactory _dbFactory;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DatabaseInitializerService> _log;

    public DatabaseInitializerService(
        AppSqliteFactory dbFactory,
        IServiceProvider serviceProvider,
        ILogger<DatabaseInitializerService> log
    )
    {
        _dbFactory = dbFactory;
        _serviceProvider = serviceProvider;
        _log = log;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Define databases to initialize
        var databases = new[]
        {
            new
            {
                Path = Environment.GetEnvironmentVariable("RULES_DB_PATH") ?? "rules.db",
                Schema = _serviceProvider.GetRequiredService<RulesDbSchema>(),
                Name = "Rules",
            },
            // TODO: Add when promoter database is needed
            // new {
            //     Path = Environment.GetEnvironmentVariable("PROMOTER_DB_PATH") ?? "promoter.db",
            //     Schema = _serviceProvider.GetRequiredService<PromoterDbSchema>(),
            //     Name = "Promoter"
            // }
        };

        foreach (var db in databases)
        {
            try
            {
                _log.LogInformation(
                    "Initializing {DatabaseName} database: {DbPath}",
                    db.Name,
                    db.Path
                );

                await using var connection = _dbFactory.Open(db.Path);
                await db.Schema.InitializeAsync(connection, cancellationToken);

                _log.LogInformation(
                    "{DatabaseName} database initialized successfully: {DbPath}",
                    db.Name,
                    db.Path
                );
            }
            catch (Exception ex)
            {
                _log.LogError(
                    ex,
                    "Failed to initialize {DatabaseName} database: {DbPath}",
                    db.Name,
                    db.Path
                );
                throw; // Fail startup if any database can't be initialized
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
