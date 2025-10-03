using System.Security.Cryptography.X509Certificates;
using System.Text.Json.Serialization;

using NATS.Client.JetStream.Models;

namespace TOOL.UseCases.SeedProposal;
public sealed record SeedDto
{
    [JsonPropertyName("event_type")] public required string EventType { get; init; }  // "im.proposal.v1"
    [JsonPropertyName("ns")] public required string Ns { get; init; }         // "ravvnen.consulting"
    [JsonPropertyName("sha")] public required string Sha { get; init; }       // "seed-test-item-v1"
    [JsonPropertyName("ci")] public required string Ci { get; init; }        // "n/a"    
    [JsonPropertyName("emitted_at")] public required DateTimeOffset EmittedAt { get; init; } // "2025-09-17T10:30:00Z"
    [JsonPropertyName("item_id")] public required string ItemId { get; init; }    // "api.auth"
    [JsonPropertyName("title")] public required string Title { get; init; }     // "API Authentication Standard"
    [JsonPropertyName("content")] public required string Content { get; init; }   // "All API endpoints MUST use OAuth2 with bearer tokens.\nReject anonymous requests with HTTP 401."
    [JsonPropertyName("labels")] public required List<string> Labels { get; init; } // ["security", "api", "standard"]
    [JsonPropertyName("source")] public required SourceInfo Source { get; init; }
    public void Validate()
    {
        if (EventType != "im.proposal.v1" ) throw new ArgumentException("Invalid EventType, must be 'im.proposal.v1'");
        if (string.IsNullOrWhiteSpace(Ns)) throw new ArgumentException("Ns is required");
        if (string.IsNullOrWhiteSpace(Sha)) throw new ArgumentException("Sha is required");
        if (string.IsNullOrWhiteSpace(Ci)) throw new ArgumentException("Ci is required");
        if (EmittedAt == default) throw new ArgumentException("EmittedAt is required");
        if (string.IsNullOrWhiteSpace(ItemId)) throw new ArgumentException("ItemId is required");
        if (string.IsNullOrWhiteSpace(Title)) throw new ArgumentException("Title is required");
        if (string.IsNullOrWhiteSpace(Content)) throw new ArgumentException("Content is required");
        Source.Validate();
    }
}


public sealed record SourceInfo
{
    [JsonPropertyName("kind")] public required string Kind { get; init; }    // "seeded" | "gh-webhook"
    [JsonPropertyName("repo_url")] public required string RepoUrl { get; init; }    // "github.com/ravvnen/test-repo"
    [JsonPropertyName("ref")] public required string Ref { get; init; }     // "main"
    [JsonPropertyName("path")] public required string Path { get; init; }    // "/test/items/test-item.yaml"
    [JsonPropertyName("blob_sha")] public required string BlobSha { get; init; }  // "abc123def456789"
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Kind) || (Kind != "seeded" && Kind != "gh-webhook"))
            throw new ArgumentException("Kind is required and must be 'seeded' or 'gh-webhook'");
        if (string.IsNullOrWhiteSpace(RepoUrl))
            throw new ArgumentException("RepoUrl is required");
        if (string.IsNullOrWhiteSpace(Ref))
            throw new ArgumentException("Ref is required");
        if (string.IsNullOrWhiteSpace(Path))
            throw new ArgumentException("Path is required");
        if (string.IsNullOrWhiteSpace(BlobSha))
            throw new ArgumentException("BlobSha is required");
    }
}