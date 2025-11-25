using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;

namespace Setup;

/// <summary>
/// Interactive onboarding helper. Call manually (e.g. when passing a --onboard flag) BEFORE starting the web host.
/// Not invoked automatically to avoid blocking the HTTP server with console input.
/// </summary>
public static class OnboardingHelper
{
    public static async Task RunAsync(IServiceProvider services, CancellationToken ct = default)
    {
        var jsContext = services.GetRequiredService<NatsJSContext>();

        Console.Write("Wipe JetStream streams and start fresh? (y/n): ");
        var wipeInput = Console.ReadLine();

        if (wipeInput?.Trim().Equals("y", StringComparison.OrdinalIgnoreCase) == true)
        {
            Console.WriteLine("Purging JetStream streams...");
            foreach (var stream in new[] { "EVENTS", "DELTAS" })
            {
                if (ct.IsCancellationRequested)
                    return;
                try
                {
                    await jsContext.PurgeStreamAsync(stream, new StreamPurgeRequest(), ct);
                    Console.WriteLine($"Purged stream {stream}");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Failed to purge stream {stream}: {e.Message}");
                }
            }
        }
    }
}
