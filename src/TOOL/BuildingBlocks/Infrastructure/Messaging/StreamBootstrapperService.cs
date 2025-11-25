using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;

namespace Infrastructure.Messaging;

public sealed class StreamBootstrapperService(
    NatsJSContext js,
    string streamName,
    string[] subjects,
    StreamConfigStorage storage = StreamConfigStorage.File,
    TimeSpan? maxAge = null
) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var streamConfig = new StreamConfig(streamName, subjects)
        {
            Storage = storage,
            Retention = StreamConfigRetention.Limits,
            // Event sourcing: unlimited retention (maxAge = 0)
            // For non-critical streams, maxAge can be set to limit retention
            MaxAge = maxAge ?? TimeSpan.Zero, // Default: unlimited (0 = forever)
        };

        // check if it exists
        try
        {
            await js.GetStreamAsync(
                streamName,
                request: null,
                cancellationToken: cancellationToken
            );
            Console.WriteLine($"[Bootstrap] Stream '{streamName}' already exists.");
            return;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch { }
        try
        {
            // create a new stream
            await js.CreateStreamAsync(streamConfig, cancellationToken);
            Console.WriteLine(
                $"[Bootstrap] Created FILE-backed stream '{streamName}' (retention: {(maxAge == TimeSpan.Zero ? "unlimited" : maxAge?.ToString() ?? "unlimited")})."
            );
            return;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to create stream '{streamName}': {ex.Message}",
                ex
            );
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
