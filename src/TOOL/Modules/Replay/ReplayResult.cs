namespace TOOL.Modules.Replay;

/// <summary>
/// Result of a replay operation
/// </summary>
public record ReplayResult(
    string Ns,
    int EventsProcessed,
    long ActiveCount,
    string ImHash,
    long ReplayTimeMs,
    DateTimeOffset StartedAt,
    DateTimeOffset CompletedAt
);

/// <summary>
/// Request to trigger a replay operation
/// </summary>
public record ReplayRequest(string? Ns = null, ulong? MaxSequence = null);

/// <summary>
/// Status of an ongoing replay operation (for async API)
/// </summary>
public enum ReplayStatus
{
    Pending,
    Running,
    Completed,
    Failed,
}

/// <summary>
/// Detailed replay job status (for async API tracking)
/// </summary>
public record ReplayJob(
    string JobId,
    string Ns,
    ReplayStatus Status,
    int? EventsProcessed,
    string? ImHash,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt,
    string? ErrorMessage
);
