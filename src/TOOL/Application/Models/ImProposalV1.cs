using System.Security.Cryptography.X509Certificates;
using NATS.Client.JetStream.Models;

public sealed record ImProposal(
    string EventType,   // "im.proposal.v1"
    string Ns,         // "ravvnen.consulting"
    string Sha,       // "seed-test-item-v1"
    string Ci,        // "n/a"
    DateTime EmittedAt,// "2025-09-17T10:30:00Z"
    string ItemId,    // "api.auth"
    string Title,     // "API Authentication Standard"
    string Content,   // "All API endpoints MUST use OAuth2 with bearer tokens.\nReject anonymous requests with HTTP 401."
    List<string> Labels, // ["security", "api", "standard"]
    SourceInfo Source     
)
{
    public void Validate()
    {
        if (EventType != "im.proposal.v1")
            throw new ArgumentException("Invalid EventType, must be 'im.proposal.v1'");
        if (string.IsNullOrWhiteSpace(Ns))
            throw new ArgumentException("Ns is required");
        if (string.IsNullOrWhiteSpace(Sha))
            throw new ArgumentException("Sha is required");
        if (string.IsNullOrWhiteSpace(Ci))
            throw new ArgumentException("Ci is required");
        if (EmittedAt == default)
            throw new ArgumentException("EmittedAt is required");
        if (string.IsNullOrWhiteSpace(ItemId))
            throw new ArgumentException("ItemId is required");
        if (string.IsNullOrWhiteSpace(Title))
            throw new ArgumentException("Title is required");
        if (string.IsNullOrWhiteSpace(Content))
            throw new ArgumentException("Content is required");
        if (Labels == null)
            throw new ArgumentException("Labels is required");
        SourceInfo source = Source ?? throw new ArgumentException("Source is required");
        Source?.Validate();
    }
}


public sealed record SourceInfo(
    string Kind,    // "seeded" | "gh-webhook"
    string RepoUrl,    // "github.com/ravvnen/test-repo"
    string Ref,     // "main"
    string Path,    // "/test/items/test-item.yaml"
    string BlobSha  // "abc123def456789"
)
{
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