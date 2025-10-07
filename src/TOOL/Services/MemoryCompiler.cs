using System.Text.RegularExpressions;
using Dapper;
using Microsoft.Data.Sqlite;

namespace TOOL.Services;

public sealed class MemoryCompiler
{
    private readonly AppSqliteFactory _dbFactory;

    public MemoryCompiler(AppSqliteFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    // Very simple scoring: FTS if exists; else keyword count + recency bump by version
    public async Task<List<MemoryItem>> SelectRelevantAsync(string ns, string prompt, int topK)
    {
        var dbPath = Environment.GetEnvironmentVariable("RULES_DB_PATH") ?? "rules.db";
        await using var db = _dbFactory.Open(dbPath);
        return await SelectRelevantAsync(db, ns, prompt, topK);
    }

    private async Task<List<MemoryItem>> SelectRelevantAsync(
        SqliteConnection db,
        string ns,
        string prompt,
        int topK
    )
    {
        var items = new List<MemoryItem>();

        var hasFts = await db.ExecuteScalarAsync<long>(
            "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='im_fts'"
        );

        if (hasFts == 1 && !string.IsNullOrWhiteSpace(prompt))
        {
            // Sanitize FTS query: extract alphanumeric tokens and join with OR
            var ftsQuery = SanitizeFtsQuery(prompt);

            if (!string.IsNullOrWhiteSpace(ftsQuery))
            {
                var rows = await db.QueryAsync<(
                    string Id,
                    string Title,
                    string Content,
                    int Version
                )>(
                    @"SELECT c.item_id as Id, c.title as Title, c.content as Content, c.version as Version
                      FROM im_fts f
                      JOIN im_items_current c ON c.ns=f.ns AND c.item_id=f.item_id
                      WHERE f.ns=@ns AND c.is_active=1 AND im_fts MATCH @q
                      LIMIT @k",
                    new
                    {
                        ns,
                        q = ftsQuery,
                        k = topK,
                    }
                );

                foreach (var r in rows)
                    items.Add(await ToItem(db, ns, r.Id, r.Title, r.Content, r.Version));
                return items;
            }
        }

        // Fallback: tokenize prompt and build OR LIKE clauses per token for recall.
        var rawTokens = Regex
            .Matches(prompt.ToLowerInvariant(), @"\w+")
            .Select(m => m.Value)
            .ToList();
        var tokensDistinct = rawTokens.Where(t => t.Length > 1).Distinct().Take(10).ToArray();

        IEnumerable<(string Id, string Title, string Content, int Version)> rows2;
        if (tokensDistinct.Length == 0)
        {
            // Nothing meaningful to split; revert to whole-prompt LIKE
            rows2 = await db.QueryAsync<(string Id, string Title, string Content, int Version)>(
                @"SELECT item_id as Id, title as Title, content as Content, version as Version
                  FROM im_items_current
                  WHERE ns=@ns AND is_active=1 AND (title LIKE @p OR content LIKE @p)
                  ORDER BY version DESC LIMIT @k",
                new
                {
                    ns,
                    p = "%" + prompt + "%",
                    k = topK * 5,
                }
            );
        }
        else
        {
            // Build dynamic OR clause
            var likeClauses = new List<string>();
            var dynParams = new DynamicParameters();
            dynParams.Add("ns", ns);
            dynParams.Add("k", topK * 5); // oversample more now that matching is more selective
            for (int i = 0; i < tokensDistinct.Length; i++)
            {
                var param = "p" + i;
                likeClauses.Add($"(title LIKE @{param} OR content LIKE @{param})");
                dynParams.Add(param, "%" + tokensDistinct[i] + "%");
            }
            var where = string.Join(" OR ", likeClauses);
            var sql =
                $@"SELECT item_id as Id, title as Title, content as Content, version as Version
                          FROM im_items_current
                          WHERE ns=@ns AND is_active=1 AND ({where})
                          ORDER BY version DESC LIMIT @k";
            rows2 = await db.QueryAsync<(string Id, string Title, string Content, int Version)>(
                sql,
                dynParams
            );
        }

        // Score & select deterministically
        var scoringTokens =
            tokensDistinct.Length > 0 ? tokensDistinct : rawTokens.Take(10).ToArray();
        var scored = rows2
            .Select(r => new
            {
                Row = r,
                Score = Score(r.Title + " " + r.Content, scoringTokens) + (r.Version * 0.01),
            })
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Row.Id)
            .Take(topK)
            .ToList();

        foreach (var s in scored)
            items.Add(await ToItem(db, ns, s.Row.Id, s.Row.Title, s.Row.Content, s.Row.Version));

        return items;
    }

    public object BuildMemoryJson(string ns, List<MemoryItem> items)
    {
        var emptyNote =
            items.Count == 0
                ? "No matching rules. Check namespace, seeding, or adjust prompt keywords."
                : null;
        return new
        {
            ns,
            generated_at = DateTimeOffset.UtcNow.ToString("o"),
            rules = items
                .Select(i => new
                {
                    id = $"im:{i.Id}",
                    title = i.Title,
                    content = i.Content,
                    provenance = new
                    {
                        repo = i.Source.Repo,
                        @ref = i.Source.Ref,
                        path = i.Source.Path,
                        blob_sha = i.Source.BlobSha,
                    },
                })
                .ToArray(),
            note = emptyNote,
        };
    }

    /// <summary>
    /// Sanitize user prompt for FTS5 MATCH query
    /// Extract alphanumeric tokens and join with OR to avoid FTS syntax errors
    /// </summary>
    static string SanitizeFtsQuery(string prompt)
    {
        var tokens = Regex
            .Matches(prompt, @"\w+")
            .Select(m => m.Value)
            .Where(t => t.Length > 1)
            .Distinct()
            .Take(10)
            .ToList();

        return tokens.Count > 0 ? string.Join(" OR ", tokens) : string.Empty;
    }

    static double Score(string text, string[] tokens)
    {
        var t = text.ToLowerInvariant();
        return tokens.Sum(tok => CountOccurrences(t, tok));
    }

    static int CountOccurrences(string hay, string needle)
    {
        if (string.IsNullOrEmpty(needle))
            return 0;
        int count = 0,
            index = 0;
        while ((index = hay.IndexOf(needle, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += needle.Length;
        }
        return count;
    }

    static async Task<MemoryItem> ToItem(
        SqliteConnection db,
        string ns,
        string id,
        string title,
        string content,
        int version
    )
    {
        var src = await db.QueryFirstOrDefaultAsync<(
            string Repo,
            string Ref,
            string Path,
            string Blob
        )>(
            @"SELECT repo as Repo, ref as Ref, path as Path, blob_sha as Blob
              FROM source_bindings
              WHERE ns=@ns AND item_id=@id AND version=@v",
            new
            {
                ns,
                id,
                v = version,
            }
        );

        return new MemoryItem(
            Id: $"{id}@v{version}",
            Title: title,
            Content: content,
            Source: new MemorySource(src.Repo, src.Ref, src.Path, src.Blob)
        );
    }
}

public record MemoryItem(string Id, string Title, string Content, MemorySource Source);

public record MemorySource(string Repo, string Ref, string Path, string BlobSha);
