using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using SQLitePCL;

namespace TOOL.UseCases.SeedProposal;

[ApiController]
[Route("api/v1")]
public class SeedController(ISeedHandler seedHandler) : ControllerBase
{
    [HttpPost("seed")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(SeedAcceptedResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ReceiveSeed(
        [FromBody] JsonElement payload,
        [FromHeader(Name = "X-GitHub-Delivery")] string githubDeliveryId, // required (non-nullable)
        [FromHeader(Name = "X-GitHub-Event")] string githubEventName, // required (non-nullable)
        CancellationToken ct = default
    )
    {
        Console.WriteLine(
            $"[Receiver] Incoming webhook: {githubEventName} (delivery: {githubDeliveryId})"
        );

        if (string.Equals(githubEventName, "ping", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine($"[Receiver] GitHub ping received - responding with pong");
            return Ok(new { pong = true });
        }

        if (payload.ValueKind != JsonValueKind.Object)
            throw new BadHttpRequestException("Invalid payload - expected JSON object");

        var raw = payload.GetRawText();

        SeedDto dto;
        try
        {
            dto =
                JsonSerializer.Deserialize<SeedDto>(raw)
                ?? throw new BadHttpRequestException("Invalid payload - could not deserialize");
            dto.Validate();
        }
        catch (JsonException jx)
        {
            Console.WriteLine($"[Receiver] Invalid JSON: {jx.Message}");
            throw new BadHttpRequestException("Invalid JSON", jx);
        }
        catch (ArgumentException ax)
        {
            Console.WriteLine($"[Receiver] Validation failed: {ax.Message}");
            throw new BadHttpRequestException("Validation failed: " + ax.Message, ax);
        }

        var result = await seedHandler.HandleAsync(dto, raw, githubDeliveryId, ct: ct);

        return Ok(new { subject = result.Subject });
    }

    public sealed record SeedAcceptedResponse(string Subject, string? Stream, ulong? Seq);
}
