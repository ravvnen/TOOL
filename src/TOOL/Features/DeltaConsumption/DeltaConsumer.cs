using NATS.Net;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;
using NATS.Client.ObjectStore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
using Delta.Consumption;
using System.Drawing;
public class DeltaConsumer : BackgroundService
{
    private readonly ILogger<DeltaConsumer> _log;
    private readonly INatsJSContext _js;

    private readonly AppSqliteFactory _sf;

    public DeltaConsumer(ILogger<DeltaConsumer> log, INatsJSContext js, AppSqliteFactory sf)
    {
        _log = log;
        _js = js;
        _sf = sf;
    }

    // Lifecycle method - runs in background
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // check if db exists, if not create

        await using var db = _sf.Open(path: "delta-im.db");

        


        // create client and consume stream
        var deltaConfig = new ConsumerConfig("deltaconsumer")
        {
            AckPolicy = ConsumerConfigAckPolicy.Explicit,
            FilterSubject = "delta.>",
        };

        var consumer = await _js.CreateOrUpdateConsumerAsync(stream: "DELTAS", config: deltaConfig, cancellationToken: stoppingToken);

        await foreach (NatsJSMsg<byte[]> msg in consumer.ConsumeAsync<byte[]>().WithCancellation(stoppingToken))
        {
            await msg.AckAsync(cancellationToken: stoppingToken);
        }
        
    }
}