namespace TOOL.UseCases.SeedProposal;

public interface ISeedHandler
{
    Task<PublishResult> HandleAsync(
        SeedDto dto,
        string rawJson,
        string? deliveryId,
        CancellationToken ct = default);
}

public readonly record struct PublishResult(bool Stored, string Subject, string? Stream, ulong? Seq);