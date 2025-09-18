using TOOL;

var builder = WebApplication.CreateBuilder(args);
builder.AddApplicationServices();

// Handle seed-only mode (no web server)
if (args.Contains("--seed-only"))
{
    var app = builder.Build();
    using var scope = app.Services.CreateScope();
    var seeder = scope.ServiceProvider.GetRequiredService<SeederService>();
    seeder.ConfirmReady();
    await seeder.SeedAllAsync();
    Console.WriteLine("[Program] Seeding complete. Exiting.");
    return;
}

var webApp = builder.Build();

// Optional interactive onboarding: run with --onboard
if (args.Contains("--onboard"))
{
    using var scope = webApp.Services.CreateScope();
    await OnboardingService.RunAsync(scope.ServiceProvider);
}

webApp.MapControllers();
webApp.Run();