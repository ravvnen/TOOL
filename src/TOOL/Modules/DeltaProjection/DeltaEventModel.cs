namespace Modules.DeltaProjection;

/// <summary>
/// Represents a parsed DELTA event message (im.upsert.v1 or im.retract.v1)
/// </summary>
public sealed record DeltaEvent
{
    public required string Type { get; init; } // "im.upsert.v1" | "im.retract.v1"
    public required string Ns { get; init; }
    public required string ItemId { get; init; }
    public required string Title { get; init; }
    public required string Content { get; init; }
    public required string LabelsJson { get; init; } // Serialized JSON array
    public required int BaseVersion { get; init; }
    public required int NewVersion { get; init; }
    public required string InputEventId { get; init; }
    public required string PolicyVersion { get; init; }
    public required DateTimeOffset OccurredAt { get; init; }
    public required DateTimeOffset EmittedAt { get; init; }

    // Source provenance
    public required string Repo { get; init; }
    public required string Ref { get; init; }
    public required string Path { get; init; }
    public required string BlobSha { get; init; }

    public bool IsUpsert => Type == "im.upsert.v1";
    public bool IsRetract => Type == "im.retract.v1";
}
