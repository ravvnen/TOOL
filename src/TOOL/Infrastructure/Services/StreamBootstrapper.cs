// StreamBootstrapper.cs
using Microsoft.Extensions.Hosting;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;

namespace TOOL;

public sealed class StreamBootstrapper(
    NatsJSContext js,
    string streamName,
    string[] subjects,
    StreamConfigStorage storage = StreamConfigStorage.File
) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var streamConfig = new StreamConfig(streamName, subjects) { Storage = storage };

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
            Console.WriteLine($"[Bootstrap] Created FILE-backed stream '{streamName}'.");
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
