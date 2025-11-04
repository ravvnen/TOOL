using System.Text.Json.Serialization;

namespace TOOL.Modules.Admin;

// ===== HTTP Request/Response DTOs =====

public sealed record CreateRuleRequest
{
    [JsonPropertyName("ns")]
    public string Ns { get; init; } = "";

    [JsonPropertyName("item_id")]
    public string ItemId { get; init; } = "";

    [JsonPropertyName("title")]
    public string Title { get; init; } = "";

    [JsonPropertyName("content")]
    public string Content { get; init; } = "";

    [JsonPropertyName("labels")]
    public string[] Labels { get; init; } = Array.Empty<string>();

    [JsonPropertyName("admin_user_id")]
    public string AdminUserId { get; init; } = "admin";

    [JsonPropertyName("reason")]
    public string Reason { get; init; } = "";
}

public sealed record UpdateRuleRequest
{
    [JsonPropertyName("title")]
    public string Title { get; init; } = "";

    [JsonPropertyName("content")]
    public string Content { get; init; } = "";

    [JsonPropertyName("labels")]
    public string[] Labels { get; init; } = Array.Empty<string>();

    [JsonPropertyName("expected_version")]
    public int? ExpectedVersion { get; init; }

    [JsonPropertyName("admin_user_id")]
    public string AdminUserId { get; init; } = "admin";

    [JsonPropertyName("reason")]
    public string Reason { get; init; } = "";
}

public sealed record DeleteRuleRequest
{
    [JsonPropertyName("expected_version")]
    public int? ExpectedVersion { get; init; }

    [JsonPropertyName("admin_user_id")]
    public string AdminUserId { get; init; } = "admin";

    [JsonPropertyName("reason")]
    public string Reason { get; init; } = "";
}

public sealed record AdminActionResponse
{
    [JsonPropertyName("accepted")]
    public bool Accepted { get; init; }

    [JsonPropertyName("subject")]
    public string Subject { get; init; } = "";

    [JsonPropertyName("event_id")]
    public string EventId { get; init; } = "";

    [JsonPropertyName("message")]
    public string? Message { get; init; }
}

// ===== Admin Event Schema (published to EVENTS stream) =====

public sealed record AdminRuleEvent
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = ""; // "Admin.RuleCreateRequested" | "Admin.RuleEditRequested" | "Admin.RuleDeleteRequested"

    [JsonPropertyName("ns")]
    public string Ns { get; init; } = "";

    [JsonPropertyName("item_id")]
    public string ItemId { get; init; } = "";

    [JsonPropertyName("action")]
    public string Action { get; init; } = ""; // "create" | "update" | "delete"

    [JsonPropertyName("title")]
    public string Title { get; init; } = "";

    [JsonPropertyName("content")]
    public string Content { get; init; } = "";

    [JsonPropertyName("labels")]
    public string[] Labels { get; init; } = Array.Empty<string>();

    [JsonPropertyName("admin_metadata")]
    public AdminMetadata AdminMetadata { get; init; } = new();

    [JsonPropertyName("source")]
    public AdminSourceInfo Source { get; init; } = new();

    [JsonPropertyName("occurred_at")]
    public DateTimeOffset OccurredAt { get; init; }

    [JsonPropertyName("event_id")]
    public string EventId { get; init; } = "";
}

public sealed record AdminMetadata
{
    [JsonPropertyName("user_id")]
    public string UserId { get; init; } = "";

    [JsonPropertyName("reason")]
    public string Reason { get; init; } = "";

    [JsonPropertyName("bypass_review")]
    public bool BypassReview { get; init; } = true; // Auto-promote by default

    [JsonPropertyName("expected_version")]
    public int? ExpectedVersion { get; init; }
}

public sealed record AdminSourceInfo
{
    [JsonPropertyName("repo")]
    public string Repo { get; init; } = "admin.override";

    [JsonPropertyName("ref")]
    public string Ref { get; init; } = "manual";

    [JsonPropertyName("path")]
    public string Path { get; init; } = "";

    [JsonPropertyName("blob_sha")]
    public string BlobSha { get; init; } = "";
}
