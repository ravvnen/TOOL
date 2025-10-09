using System.Text;
using NATS.Client.Core;
using NATS.Client.JetStream;

namespace TOOL.Modules.SeedProcessing;

public sealed class SeedHandler : ISeedHandler
{
    private readonly NatsJSContext _js;
    private readonly ILogger<SeedHandler> _log;

    public SeedHandler(NatsJSContext js, ILogger<SeedHandler> log)
    {
        _js = js;
        _log = log;
    }

    public async Task<PublishResult> HandleAsync(
        SeedDto dto,
        string rawJson,
        string? deliveryId,
        CancellationToken ct = default
    )
    {
        dto.Validate();

        var orgRepo = !string.IsNullOrWhiteSpace(dto.Source.RepoUrl)
            ? dto.Source.RepoUrl
            : ExtractOrgRepoFromUrl(dto.Source.RepoUrl)
                ?? throw new BadHttpRequestException("source.repo or source.repoUrl required");

        var (org, repo) = SplitOrgRepoOrBadRequest(orgRepo);

        org = SanitizeToken(org);
        repo = SanitizeToken(repo);

        const string prefix = "evt";
        var subject = $"{prefix}.{org}.{repo}.seed.proposal";

        var msgId = !string.IsNullOrWhiteSpace(deliveryId)
            ? $"seed-{deliveryId}"
            : $"seed-{dto.Sha}-{dto.ItemId}";

        var headers = new NatsHeaders
        {
            { "Nats-Msg-Id", msgId },
            { "Content-Type", "application/json" },
        };

        var ack = await _js.PublishAsync(
            subject,
            Encoding.UTF8.GetBytes(rawJson),
            headers: headers,
            cancellationToken: ct
        );

        _log.LogInformation(
            "[Seed] Published {Subject} (stream={Stream}, seq={Seq})",
            subject,
            ack.Stream,
            ack.Seq
        );
        return new PublishResult(true, subject, ack.Stream, ack.Seq);
    }

    private static (string org, string repo) SplitOrgRepoOrBadRequest(string value)
    {
        var parts = value.Split('/', 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
            throw new BadHttpRequestException("source.repo must be 'org/repo'");
        return (parts[0], parts[1]);
    }

    private static string? ExtractOrgRepoFromUrl(string? repoUrl)
    {
        if (string.IsNullOrWhiteSpace(repoUrl))
            return null;
        var url = repoUrl.Contains("://", StringComparison.Ordinal)
            ? repoUrl
            : "https://" + repoUrl;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var u))
            return null;
        var segs = u.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segs.Length < 2)
            return null;
        var org = segs[0];
        var repo = segs[1].EndsWith(".git", StringComparison.OrdinalIgnoreCase)
            ? segs[1][..^4]
            : segs[1];
        return $"{org}/{repo}";
    }

    private static string SanitizeToken(string token)
    {
        Span<char> buf = stackalloc char[token.Length];
        for (int i = 0; i < token.Length; i++)
        {
            var c = token[i];
            buf[i] = (c == '.' || c == '*' || c == '>' || c == ' ' || c == '\t') ? '-' : c;
        }
        return new string(buf);
    }
}
