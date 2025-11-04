using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using NATS.Client.Core;
using NATS.Client.JetStream;

namespace TOOL.Modules.Admin;

[ApiController]
[Route("api/v1/admin")]
public class AdminController : ControllerBase
{
    private readonly NatsJSContext _js;
    private readonly ILogger<AdminController> _log;

    public AdminController(NatsJSContext js, ILogger<AdminController> log)
    {
        _js = js;
        _log = log;
    }

    /// <summary>
    /// Create a new rule via admin override (publishes Admin.RuleCreateRequested to EVENTS)
    /// </summary>
    [HttpPost("rules")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(AdminActionResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateRule(
        [FromBody] CreateRuleRequest request,
        CancellationToken ct = default
    )
    {
        // Validate
        if (string.IsNullOrWhiteSpace(request.Ns))
            return BadRequest(new { error = "ns is required" });
        if (string.IsNullOrWhiteSpace(request.ItemId))
            return BadRequest(new { error = "item_id is required" });
        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest(new { error = "title is required" });
        if (string.IsNullOrWhiteSpace(request.Content))
            return BadRequest(new { error = "content is required" });
        if (string.IsNullOrWhiteSpace(request.Reason))
            return BadRequest(new { error = "reason is required" });

        var eventId = Guid.NewGuid().ToString("N");
        var occurredAt = DateTimeOffset.UtcNow;

        // Build admin event
        var adminEvent = new AdminRuleEvent
        {
            Type = "Admin.RuleCreateRequested",
            Ns = request.Ns,
            ItemId = request.ItemId,
            Action = "create",
            Title = request.Title,
            Content = request.Content,
            Labels = request.Labels ?? Array.Empty<string>(),
            AdminMetadata = new AdminMetadata
            {
                UserId = request.AdminUserId,
                Reason = request.Reason,
                BypassReview = true, // Auto-promote admin edits
                ExpectedVersion = null, // New rule, no version check
            },
            Source = new AdminSourceInfo
            {
                Repo = "admin.override",
                Ref = "manual",
                Path = $"admin/{request.AdminUserId}/{occurredAt:yyyy-MM-ddTHH:mm:ssZ}",
                BlobSha = ComputeSha256(request.Content),
            },
            OccurredAt = occurredAt,
            EventId = eventId,
        };

        // Publish to EVENTS stream (must match evt.> pattern)
        var subject = $"evt.admin.rule.create.{request.Ns}.{request.ItemId}";
        var result = await PublishAdminEventAsync(subject, adminEvent, eventId, ct);

        return AcceptedAtAction(
            null,
            new AdminActionResponse
            {
                Accepted = true,
                Subject = result.Subject,
                EventId = eventId,
                Message = "Admin rule creation request published to EVENTS stream",
            }
        );
    }

    /// <summary>
    /// Update an existing rule via admin override (publishes Admin.RuleEditRequested to EVENTS)
    /// </summary>
    [HttpPut("rules/{itemId}")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(AdminActionResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateRule(
        string itemId,
        [FromBody] UpdateRuleRequest request,
        [FromQuery] string ns = "ravvnen.consulting",
        CancellationToken ct = default
    )
    {
        // Validate
        if (string.IsNullOrWhiteSpace(itemId))
            return BadRequest(new { error = "item_id is required" });
        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest(new { error = "title is required" });
        if (string.IsNullOrWhiteSpace(request.Content))
            return BadRequest(new { error = "content is required" });
        if (string.IsNullOrWhiteSpace(request.Reason))
            return BadRequest(new { error = "reason is required" });

        var eventId = Guid.NewGuid().ToString("N");
        var occurredAt = DateTimeOffset.UtcNow;

        // Build admin event
        var adminEvent = new AdminRuleEvent
        {
            Type = "Admin.RuleEditRequested",
            Ns = ns,
            ItemId = itemId,
            Action = "update",
            Title = request.Title,
            Content = request.Content,
            Labels = request.Labels ?? Array.Empty<string>(),
            AdminMetadata = new AdminMetadata
            {
                UserId = request.AdminUserId,
                Reason = request.Reason,
                BypassReview = true,
                ExpectedVersion = request.ExpectedVersion, // Conflict detection
            },
            Source = new AdminSourceInfo
            {
                Repo = "admin.override",
                Ref = "manual",
                Path = $"admin/{request.AdminUserId}/{occurredAt:yyyy-MM-ddTHH:mm:ssZ}",
                BlobSha = ComputeSha256(request.Content),
            },
            OccurredAt = occurredAt,
            EventId = eventId,
        };

        // Publish to EVENTS stream (must match evt.> pattern)
        var subject = $"evt.admin.rule.edit.{ns}.{itemId}";
        var result = await PublishAdminEventAsync(subject, adminEvent, eventId, ct);

        return AcceptedAtAction(
            null,
            new AdminActionResponse
            {
                Accepted = true,
                Subject = result.Subject,
                EventId = eventId,
                Message = "Admin rule edit request published to EVENTS stream",
            }
        );
    }

    /// <summary>
    /// Delete (retract) a rule via admin override (publishes Admin.RuleDeleteRequested to EVENTS)
    /// </summary>
    [HttpDelete("rules/{itemId}")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(AdminActionResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteRule(
        string itemId,
        [FromBody] DeleteRuleRequest request,
        [FromQuery] string ns = "ravvnen.consulting",
        CancellationToken ct = default
    )
    {
        // Validate
        if (string.IsNullOrWhiteSpace(itemId))
            return BadRequest(new { error = "item_id is required" });
        if (string.IsNullOrWhiteSpace(request.Reason))
            return BadRequest(new { error = "reason is required" });

        var eventId = Guid.NewGuid().ToString("N");
        var occurredAt = DateTimeOffset.UtcNow;

        // Build admin event (minimal payload for delete)
        var adminEvent = new AdminRuleEvent
        {
            Type = "Admin.RuleDeleteRequested",
            Ns = ns,
            ItemId = itemId,
            Action = "delete",
            Title = "", // Not needed for delete
            Content = "", // Not needed for delete
            Labels = Array.Empty<string>(),
            AdminMetadata = new AdminMetadata
            {
                UserId = request.AdminUserId,
                Reason = request.Reason,
                BypassReview = true,
                ExpectedVersion = request.ExpectedVersion, // Optimistic locking
            },
            Source = new AdminSourceInfo
            {
                Repo = "admin.override",
                Ref = "manual",
                Path = $"admin/{request.AdminUserId}/{occurredAt:yyyy-MM-ddTHH:mm:ssZ}",
                BlobSha = "", // No content for delete
            },
            OccurredAt = occurredAt,
            EventId = eventId,
        };

        // Publish to EVENTS stream (must match evt.> pattern)
        var subject = $"evt.admin.rule.delete.{ns}.{itemId}";
        var result = await PublishAdminEventAsync(subject, adminEvent, eventId, ct);

        return AcceptedAtAction(
            null,
            new AdminActionResponse
            {
                Accepted = true,
                Subject = result.Subject,
                EventId = eventId,
                Message = "Admin rule delete request published to EVENTS stream",
            }
        );
    }

    // ===== Helper Methods =====

    private async Task<(string Subject, string Stream, ulong Seq)> PublishAdminEventAsync(
        string subject,
        AdminRuleEvent adminEvent,
        string eventId,
        CancellationToken ct
    )
    {
        var json = JsonSerializer.Serialize(
            adminEvent,
            new JsonSerializerOptions { WriteIndented = false }
        );

        var headers = new NatsHeaders
        {
            { "Nats-Msg-Id", $"admin-{eventId}" },
            { "Content-Type", "application/json" },
        };

        var ack = await _js.PublishAsync(
            subject,
            Encoding.UTF8.GetBytes(json),
            headers: headers,
            cancellationToken: ct
        );

        _log.LogInformation(
            "[Admin] Published {Subject} (stream={Stream}, seq={Seq}, eventId={EventId})",
            subject,
            ack.Stream,
            ack.Seq,
            eventId
        );

        return (subject, ack.Stream ?? "EVENTS", ack.Seq);
    }

    private static string ComputeSha256(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }
}
