using TOOL;
using TOOL.Modules.SeedProcessing.Ingestion;

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

// Optional interactive onboarding: run with --onboard
// TODO: OnboardingHelper needs to be located and imported
// if (args.Contains("--onboard"))
// {
//     using var scope = webApp.Services.CreateScope();
//     await OnboardingHelper.RunAsync(scope.ServiceProvider);
// }

// Enable CORS for Agent.Container communication
webApp.UseCors();
webApp.MapControllers();
webApp.Run();

// Make Program accessible for integration tests
public partial class Program { }
