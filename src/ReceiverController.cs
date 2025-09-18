using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace TOOL;
[ApiController]
[Route("api/webhooks/github/v1")]
public class ReceiverController(IWebhookService webhookService) : ControllerBase
{
    [HttpPost]
    [Consumes("application/json")]
    public async Task<IActionResult> Receive(
        [FromBody] JsonElement payload,
        [FromHeader(Name = "X-GitHub-Delivery")] string deliveryId,  // required (non-nullable)
        [FromHeader(Name = "X-GitHub-Event")] string eventName,      // required (non-nullable)
        CancellationToken ct)
    {
        Console.WriteLine($"[Receiver] Incoming webhook: {eventName} (delivery: {deliveryId})");
        
        // quick ignore for GitHub "ping" handshakes
        if (string.Equals(eventName, "ping", StringComparison.OrdinalIgnoreCase))
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
        var result = await webhookService.HandleAsync(payload, raw, eventName, deliveryId, ct);


        return Ok(new { subject = result.Subject });
    }
}
