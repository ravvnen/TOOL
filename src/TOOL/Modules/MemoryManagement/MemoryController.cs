using System.Text.Json;
using Dapper;
using Infrastructure.Database;
using Microsoft.AspNetCore.Mvc;

namespace Modules.MemoryManagement;

[ApiController]
[Route("api/v1")]
public class MemoryController : ControllerBase
{
    private readonly MemoryCompiler _memoryCompiler;
    private readonly AppSqliteFactory _dbFactory;
    private readonly ILogger<MemoryController> _log;

    public MemoryController(
        MemoryCompiler memoryCompiler,
        AppSqliteFactory dbFactory,
        ILogger<MemoryController> log
    )
    {
        _memoryCompiler = memoryCompiler;
        _dbFactory = dbFactory;
        _log = log;
    }

    /// <summary>
    /// Compile memory: fetch relevant rules based on prompt
    /// </summary>
    [HttpPost("compile-memory")]
    [ProducesResponseType(typeof(CompiledMemoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CompileMemory([FromBody] CompileMemoryRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Prompt))
            return BadRequest(new { error = "prompt required" });

        var ns = req.Ns ?? "ravvnen.consulting";
        var topK = req.TopK ?? 6;

        try
        {
            // Select relevant rules
            var selected = await _memoryCompiler.SelectRelevantAsync(ns, req.Prompt, topK);

            // Build structured memory JSON
            var memory = _memoryCompiler.BuildMemoryJson(ns, selected);

            _log.LogInformation(
                "Compiled memory for ns={ns}, prompt={prompt}, found {count} rules",
                ns,
                req.Prompt.Substring(0, Math.Min(50, req.Prompt.Length)),
                selected.Count
            );

            return Ok(memory);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error compiling memory for ns={ns}", ns);
            return StatusCode(500, new { error = "Failed to compile memory", detail = ex.Message });
        }
    }

    /// <summary>
    /// Get state: count and hash of active rules
    /// </summary>
    [HttpGet("state")]
    [ProducesResponseType(typeof(StateResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetState([FromQuery] string? ns)
    {
        var namespace_ = ns ?? "ravvnen.consulting";
        var dbPath = Environment.GetEnvironmentVariable("RULES_DB_PATH") ?? "rules.db";

        try
        {
            await using var db = _dbFactory.Open(dbPath);

            var count = await db.ExecuteScalarAsync<long>(
                "SELECT COUNT(*) FROM im_items_current WHERE ns=@ns AND is_active=1",
                new { ns = namespace_ }
            );

            var rows = await db.QueryAsync<(string Title, string Content)>(
                "SELECT title,content FROM im_items_current WHERE ns=@ns AND is_active=1 ORDER BY item_id",
                new { ns = namespace_ }
            );

            var text = string.Join("\n---\n", rows.Select(r => r.Title + "\n" + r.Content));
            var hash = HexSha256(text);

            return Ok(new StateResponse(namespace_, count, hash));
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error getting state for ns={ns}", namespace_);
            return StatusCode(500, new { error = "Failed to get state", detail = ex.Message });
        }
    }

    /// <summary>
    /// Get provenance (why) for a specific rule
    /// </summary>
    [HttpGet("why")]
    [ProducesResponseType(typeof(ProvenanceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Why([FromQuery] string id, [FromQuery] string? ns)
    {
        if (string.IsNullOrWhiteSpace(id))
            return BadRequest(new { error = "id required" });

        var namespace_ = ns ?? "ravvnen.consulting";
        var dbPath = Environment.GetEnvironmentVariable("RULES_DB_PATH") ?? "rules.db";

        try
        {
            await using var db = _dbFactory.Open(dbPath);

            var cur = await db.QueryFirstOrDefaultAsync(
                "SELECT item_id,title,content,version FROM im_items_current WHERE ns=@ns AND item_id=@id",
                new { ns = namespace_, id }
            );

            if (cur is null)
                return NotFound(new { error = "not found" });

            var src = await db.QueryAsync(
                "SELECT repo,ref,path,blob_sha FROM source_bindings WHERE ns=@ns AND item_id=@id AND version=@v",
                new
                {
                    ns = namespace_,
                    id,
                    v = (int)cur.version,
                }
            );

            return Ok(
                new ProvenanceResponse(
                    cur.item_id,
                    cur.title,
                    cur.content,
                    (int)cur.version,
                    src.ToList()
                )
            );
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error getting provenance for id={id}, ns={ns}", id, namespace_);
            return StatusCode(500, new { error = "Failed to get provenance", detail = ex.Message });
        }
    }

    /// <summary>
    /// Search rules (FTS5 if available, else LIKE)
    /// </summary>
    [HttpGet("search")]
    [ProducesResponseType(typeof(IEnumerable<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search(
        [FromQuery] string q,
        [FromQuery] int k = 10,
        [FromQuery] string? ns = null
    )
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest(new { error = "query (q) required" });

        var namespace_ = ns ?? "ravvnen.consulting";
        var dbPath = Environment.GetEnvironmentVariable("RULES_DB_PATH") ?? "rules.db";

        try
        {
            await using var db = _dbFactory.Open(dbPath);

            var hasFts = await db.ExecuteScalarAsync<long>(
                "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='im_fts'"
            );

            object rows;
            if (hasFts == 1)
            {
                // Sanitize FTS query to avoid syntax errors
                var ftsQuery = SanitizeFtsQuery(q);
                rows = await db.QueryAsync(
                    @"SELECT c.item_id,c.title,c.content,c.version
                      FROM im_fts f
                      JOIN im_items_current c ON c.ns=f.ns AND c.item_id=f.item_id
                      WHERE f.ns=@ns AND c.is_active=1 AND im_fts MATCH @q
                      LIMIT @k",
                    new
                    {
                        ns = namespace_,
                        q = ftsQuery,
                        k,
                    }
                );
            }
            else
            {
                rows = await db.QueryAsync(
                    @"SELECT item_id,title,content,version
                      FROM im_items_current WHERE ns=@ns AND is_active=1
                        AND (title LIKE @p OR content LIKE @p)
                      LIMIT @k",
                    new
                    {
                        ns = namespace_,
                        p = "%" + q + "%",
                        k,
                    }
                );
            }

            return Ok(rows);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error searching for q={q}, ns={ns}", q, namespace_);
            return StatusCode(500, new { error = "Failed to search", detail = ex.Message });
        }
    }

    /// <summary>
    /// Debug: List all current items
    /// </summary>
    [HttpGet("debug/items")]
    [ProducesResponseType(typeof(IEnumerable<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DebugItems([FromQuery] string? ns)
    {
        var namespace_ = ns ?? "ravvnen.consulting";
        var dbPath = Environment.GetEnvironmentVariable("RULES_DB_PATH") ?? "rules.db";

        try
        {
            await using var db = _dbFactory.Open(dbPath);
            var rows = await db.QueryAsync(
                @"
                SELECT ns, item_id, version, title, substr(content,1,300) AS preview,
                       labels_json, is_active, policy_version, occurred_at, emitted_at
                FROM im_items_current
                WHERE ns=@ns
                ORDER BY item_id",
                new { ns = namespace_ }
            );
            return Ok(rows);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error getting debug items for ns={ns}", namespace_);
            return StatusCode(500, new { error = "Failed to get items", detail = ex.Message });
        }
    }

    /// <summary>
    /// Debug: Get full detail for a single item (current + history + provenance)
    /// </summary>
    [HttpGet("debug/item/{id}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> DebugItem(string id, [FromQuery] string? ns)
    {
        var namespace_ = ns ?? "ravvnen.consulting";
        var dbPath = Environment.GetEnvironmentVariable("RULES_DB_PATH") ?? "rules.db";

        try
        {
            await using var db = _dbFactory.Open(dbPath);

            var cur = await db.QueryFirstOrDefaultAsync(
                @"
                SELECT ns, item_id, version, title, content, labels_json,
                       is_active, policy_version, occurred_at, emitted_at
                FROM im_items_current WHERE ns=@ns AND item_id=@id",
                new { ns = namespace_, id }
            );

            var history = await db.QueryAsync(
                @"
                SELECT version, title, substr(content,1,300) AS preview, labels_json,
                       is_active, policy_version, occurred_at, emitted_at
                FROM im_items_history WHERE ns=@ns AND item_id=@id
                ORDER BY version ASC",
                new { ns = namespace_, id }
            );

            var prov = await db.QueryAsync(
                @"
                SELECT version, repo, ref, path, blob_sha
                FROM source_bindings WHERE ns=@ns AND item_id=@id
                ORDER BY version ASC",
                new { ns = namespace_, id }
            );

            return Ok(
                new
                {
                    current = cur,
                    history,
                    provenance = prov,
                }
            );
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error getting debug item id={id}, ns={ns}", id, namespace_);
            return StatusCode(
                500,
                new { error = "Failed to get item detail", detail = ex.Message }
            );
        }
    }

    /// <summary>
    /// Debug: Get all source bindings
    /// </summary>
    [HttpGet("debug/sources")]
    [ProducesResponseType(typeof(IEnumerable<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DebugSources([FromQuery] string? ns)
    {
        var namespace_ = ns ?? "ravvnen.consulting";
        var dbPath = Environment.GetEnvironmentVariable("RULES_DB_PATH") ?? "rules.db";

        try
        {
            await using var db = _dbFactory.Open(dbPath);
            var rows = await db.QueryAsync(
                @"
                SELECT ns, item_id, version, repo, ref, path, blob_sha
                FROM source_bindings WHERE ns=@ns
                ORDER BY item_id, version",
                new { ns = namespace_ }
            );
            return Ok(rows);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error getting debug sources for ns={ns}", namespace_);
            return StatusCode(500, new { error = "Failed to get sources", detail = ex.Message });
        }
    }

    private static string HexSha256(string s)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        return Convert.ToHexString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(s)));
    }

    /// <summary>
    /// Sanitize user query for FTS5 MATCH - extract alphanumeric tokens and join with OR
    /// </summary>
    private static string SanitizeFtsQuery(string query)
    {
        var tokens = System
            .Text.RegularExpressions.Regex.Matches(query, @"\w+")
            .Select(m => m.Value)
            .Where(t => t.Length > 1)
            .Distinct()
            .Take(10)
            .ToList();

        return tokens.Count > 0 ? string.Join(" OR ", tokens) : query;
    }
}

// === DTOs ===

public record CompileMemoryRequest(string Prompt, int? TopK, string? Ns);

public record CompiledMemoryResponse(string Ns, string GeneratedAt, object[] Rules, string? Note);

public record StateResponse(string Ns, long ActiveItems, string ImHash);

public record ProvenanceResponse(
    string Id,
    string Title,
    string Content,
    int Version,
    List<dynamic> Sources
);
