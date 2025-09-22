using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace TOOL;

[ApiController]
[Route("api/v1")]
public class ReceiverController(IWebhookService webhookService) : ControllerBase
{
    [HttpPost("seed-proposal")]
    public async Task<IActionResult> SeededProposal(

        [FromBody] JsonElement payload,
        [FromHeader(Name = "X-GitHub-Delivery")] string githubDeliveryId,  // required (non-nullable)
        [FromHeader(Name = "X-GitHub-Event")] string githubEventName,      // required (non-nullable)
        CancellationToken ct)
    {
        Console.WriteLine($"[Receiver] Incoming webhook: {githubEventName} (delivery: {githubDeliveryId})");

        // quick ignore for GitHub "ping" handshakes
        if (string.Equals(githubEventName, "ping", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine($"[Receiver] GitHub ping received - responding with pong");
            return Ok(new { pong = true });
        }

        // basic body check
        if (payload.ValueKind == JsonValueKind.Undefined || payload.ValueKind == JsonValueKind.Null)
            return BadRequest(new { error = "Empty or invalid JSON body" });

        // raw JSON for publishing / (future) HMAC verification
        var raw = payload.GetRawText();

        // hand off to app/service layer (maps subject + publishes)
        var result = await webhookService.HandleAsync(payload, raw, githubEventName, githubDeliveryId, ct);
        return Ok(new { subject = result.Subject });
    }
}
