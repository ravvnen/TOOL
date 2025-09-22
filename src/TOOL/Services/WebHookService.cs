using System.Text;
using System.Text.Json;
using NATS.Client.Core;
using NATS.Client.JetStream;

namespace TOOL;

public interface IWebhookService
{
    Task<PublishResult> HandleAsync(
        JsonElement body,
        string rawBody,
        string eventName,
        string deliveryId,
        CancellationToken ct);
}

public sealed class WebhookService(NatsJSContext js) : IWebhookService
{
    public async Task<PublishResult> HandleAsync(
        JsonElement body,
        string rawBody,
        string eventName,
        string deliveryId,
        CancellationToken ct)
    {
        // 1) accept only the events you handle for now , which is seeding and pushes
        if (!string.Equals(eventName, "push", StringComparison.OrdinalIgnoreCase) && 
            !string.Equals(eventName, "seeded_proposal", StringComparison.OrdinalIgnoreCase))
            throw new BadHttpRequestException($"Unsupported X-GitHub-Event '{eventName}'");

        // 2) map to a subject (dynamic-safe) - handle both GitHub webhook and seeded formats
        string org, repo;
        
        if (string.Equals(eventName, "seeded_proposal", StringComparison.OrdinalIgnoreCase))
        {
            // Validation for seeded proposals
            if (!body.TryGetProperty("ns", out var nsEl))    throw new BadHttpRequestException("ns required");
            if (!body.TryGetProperty("sha", out var shaEl))  throw new BadHttpRequestException("sha required");
            if (!body.TryGetProperty("item_id", out var idEl)) throw new BadHttpRequestException("item_id required");
            if (!body.TryGetProperty("title", out _))        throw new BadHttpRequestException("title required");
            if (!body.TryGetProperty("content", out _))      throw new BadHttpRequestException("content required");
            if (!body.TryGetProperty("source", out _))       throw new BadHttpRequestException("source required");
            
            // For seeded events, extract from source.repo format
            if (!body.TryGetProperty("source", out var sourceEl) ||
                !sourceEl.TryGetProperty("repo", out var sourceRepoEl))
                throw new BadHttpRequestException("Missing source.repo for seeded event");
                
            var sourceRepo = sourceRepoEl.GetString() ?? throw new BadHttpRequestException("source.repo is null");
            // Handle both "org/repo" and just "repo" formats
            if (sourceRepo.Contains('/'))
            {
                var parts = sourceRepo.Split('/', 2);
                org = parts[0];
                repo = parts[1];
            }
            else
            throw new BadHttpRequestException("source.repo must be in 'org/repo' format");
        }
        else
        {
            // For GitHub webhook events, use standard format
            if (!body.TryGetProperty("repository", out var repoEl) ||
                !repoEl.TryGetProperty("name", out var repoNameEl) ||
                !repoEl.TryGetProperty("owner", out var ownerEl) ||
                !ownerEl.TryGetProperty("login", out var orgEl))
                throw new BadHttpRequestException("Missing repository.owner.login or repository.name");

            org = orgEl.GetString() ?? "unknown-org";
            repo = repoNameEl.GetString() ?? "unknown-repo";
        }
        
        // Handle different event types
        string eventType;
        if (string.Equals(eventName, "push", StringComparison.OrdinalIgnoreCase))
        {
            // For push events, check if it's to main branch
            var refName = body.TryGetProperty("ref", out var refEl) ? refEl.GetString() : null;
            var isMainBranch = refName?.EndsWith("/main") == true || refName?.EndsWith("/master") == true;
            eventType = isMainBranch ? "push.main" : "push.branch";
        }
        else if (string.Equals(eventName, "seeded_proposal", StringComparison.OrdinalIgnoreCase))
        {
            // For seeded proposals, use the item_id or type from the payload
            var itemType = body.TryGetProperty("type", out var typeEl) ? typeEl.GetString() : "proposal";
            eventType = $"seed.{itemType}";
        }
        else
        {
            eventType = "unknown";
        }

        var subject = $"evt.{org}.{repo}.{eventType}";  // stream binds evt.*.*.*

        // 3) publish to JetStream (durable) with idempotency
        var bytes = Encoding.UTF8.GetBytes(rawBody);
        NatsHeaders? headers = null;
        
        // Compute deterministic Msg-Id for idempotency
        string msgId;
        if (string.Equals(eventName, "seeded_proposal", StringComparison.OrdinalIgnoreCase))
        {
            // For seeded content, use the deterministic sha field for idempotency
            var sha = body.TryGetProperty("sha", out var shaEl) ? shaEl.GetString() : "unknown-sha";
            var itemId = body.TryGetProperty("item_id", out var idEl) ? idEl.GetString() : "unknown-item";
            msgId = $"seed-{sha}-{itemId}";
        }
        else
        {
            // For GitHub webhooks, use delivery ID if available, otherwise generate one
            msgId = !string.IsNullOrEmpty(deliveryId) ? $"github-{deliveryId}" : $"event-{Guid.NewGuid()}";
        }
        
        headers = new NatsHeaders();
        headers.Add("Nats-Msg-Id", msgId); // dedupe

        var ack = await js.PublishAsync(
            subject,
            bytes,
            headers: headers,
            cancellationToken: ct);
        
        Console.WriteLine($"[Webhook] Published to '{subject}' (stream={ack.Stream}, seq={ack.Seq})");
        return new PublishResult(true, subject, ack.Stream, ack.Seq);
    }
}

public readonly record struct PublishResult(bool Stored, string Subject, string? Stream, ulong? Seq);
