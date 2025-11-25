using Configuration;
using Modules.SeedProcessing;

var builder = WebApplication.CreateBuilder(args);
builder.AddApplicationServices();

// Handle seed-only mode (no web server)
if (args.Contains("--seed-only"))
{
    var app = builder.Build();
    using var scope = app.Services.CreateScope();
    var seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
    seeder.ConfirmReady();
    await seeder.SeedAllAsync();
    Console.WriteLine("[Program] Seeding complete. Exiting.");
    return;
}

var webApp = builder.Build();

// Enable CORS for Agent.Container communication
webApp.UseCors();
webApp.MapControllers();
webApp.Run();

// Make Program accessible for integration tests
public partial class Program { }
