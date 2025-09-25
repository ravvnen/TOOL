using System.Net.Http.Json;
using System.Text.Json;
using Agent.Container;
using Dapper;
using Microsoft.Data.Sqlite;
using NATS.Client.Core;
using NATS.Client.JetStream;

var builder = WebApplication.CreateBuilder(args);

// Load appsettings + env
builder.Configuration
    .AddJsonFile("appsettings.json", optional:true, reloadOnChange:true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional:true, reloadOnChange:true)
    .AddEnvironmentVariables();

// --- Config ---
var cfg = new AgentConfig(
    AgentName: Environment.GetEnvironmentVariable("AGENT_NAME") ?? $"agent-{Environment.MachineName}",
    Ns:        Environment.GetEnvironmentVariable("NS") ?? "ravvnen.consulting",
    NatsUrl:   Environment.GetEnvironmentVariable("NATS_URL") ?? "nats://localhost:4222",
    DbPath:    Environment.GetEnvironmentVariable("AGENT_DB_PATH") ?? "agent-im.db",
    LlmUrl:    Environment.GetEnvironmentVariable("LLM_URL") ?? "http://localhost:11434/v1/chat/completions" // optional
);
builder.Services.AddSingleton(cfg);

// --- Logging (explicit) ---
// Ensure we always see our application logs (including BackgroundService) on the console.
builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(o => {
    o.SingleLine = true;
    o.TimestampFormat = "HH:mm:ss ";
});
builder.Logging.SetMinimumLevel(LogLevel.Debug); // capture Information + Debug

// --- NATS JetStream ctx ---
builder.Services.AddSingleton<NatsJSContext>(_ => {
    var nats = new NatsConnection(new NatsOpts { Url = cfg.NatsUrl });
    return new NatsJSContext(nats);
});

// --- Services ---
builder.Services.AddSingleton<AppSqliteFactory>();
builder.Services.AddSingleton<MemoryCompiler>();
builder.Services.AddHostedService<DeltaConsumerService>();

// --- Web ---
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();
app.UseCors();
app.UseDefaultFiles();  // will serve wwwroot/index.html
app.UseStaticFiles();

// Health
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

// State (hash, count)
app.MapGet("/state", async (AgentConfig cfg, AppSqliteFactory sf) =>
{
    await using var db = sf.Open(cfg.DbPath);
    var count = await db.ExecuteScalarAsync<long>(
        "SELECT COUNT(*) FROM im_items_current WHERE ns=@ns AND is_active=1", new { ns = cfg.Ns });

    var rows = await db.QueryAsync<(string Title,string Content)>(
        "SELECT title,content FROM im_items_current WHERE ns=@ns AND is_active=1 ORDER BY item_id",
        new { ns = cfg.Ns });

    var text = string.Join("\n---\n", rows.Select(r => r.Title + "\n" + r.Content));
    var hash = Util.HexSha256(text);
    return Results.Ok(new { ns = cfg.Ns, active_items = count, im_hash = hash });
});

// Why (provenance)
app.MapGet("/why", async (string id, AgentConfig cfg, AppSqliteFactory sf) =>
{
    await using var db = sf.Open(cfg.DbPath);
    var cur = await db.QueryFirstOrDefaultAsync(
        "SELECT item_id,title,content,version FROM im_items_current WHERE ns=@ns AND item_id=@id",
        new { ns = cfg.Ns, id });
    if (cur is null) return Results.NotFound(new { error = "not found" });

    var src = await db.QueryAsync(
        "SELECT repo,ref,path,blob_sha FROM source_bindings WHERE ns=@ns AND item_id=@id AND version=@v",
        new { ns = cfg.Ns, id, v = (int)cur.version });

    return Results.Ok(new {
        id = cur.item_id, title = cur.title, content = cur.content,
        version = cur.version, sources = src
    });
});

// === DEBUG / TRANSPARENCY ===

// All current items
app.MapGet("/debug/items", async (AgentConfig cfg, AppSqliteFactory sf) =>
{
    await using var db = sf.Open(cfg.DbPath);
    var rows = await db.QueryAsync(@"
        SELECT ns, item_id, version, title, substr(content,1,300) AS preview,
               labels_json, is_active, policy_version, occurred_at, emitted_at
        FROM im_items_current
        WHERE ns=@ns
        ORDER BY item_id", new { ns = cfg.Ns });
    return Results.Ok(rows);
});

// Single item detail (current + history + provenance)
app.MapGet("/debug/item/{id}", async (string id, AgentConfig cfg, AppSqliteFactory sf) =>
{
    await using var db = sf.Open(cfg.DbPath);
    var cur = await db.QueryFirstOrDefaultAsync(@"
        SELECT ns, item_id, version, title, content, labels_json,
               is_active, policy_version, occurred_at, emitted_at
        FROM im_items_current WHERE ns=@ns AND item_id=@id",
        new { ns = cfg.Ns, id });

    var history = await db.QueryAsync(@"
        SELECT version, title, substr(content,1,300) AS preview, labels_json,
               is_active, policy_version, occurred_at, emitted_at
        FROM im_items_history WHERE ns=@ns AND item_id=@id
        ORDER BY version ASC", new { ns = cfg.Ns, id });

    var prov = await db.QueryAsync(@"
        SELECT version, repo, ref, path, blob_sha
        FROM source_bindings WHERE ns=@ns AND item_id=@id
        ORDER BY version ASC", new { ns = cfg.Ns, id });

    return Results.Ok(new { current = cur, history, provenance = prov });
});

// Raw provenance (all)
app.MapGet("/debug/sources", async (AgentConfig cfg, AppSqliteFactory sf) =>
{
    await using var db = sf.Open(cfg.DbPath);
    var rows = await db.QueryAsync(@"
        SELECT ns, item_id, version, repo, ref, path, blob_sha
        FROM source_bindings WHERE ns=@ns
        ORDER BY item_id, version", new { ns = cfg.Ns });
    return Results.Ok(rows);
});

// Search (FTS if available, else LIKE)
app.MapGet("/search", async (string q, int k, AgentConfig cfg, AppSqliteFactory sf) =>
{
    await using var db = sf.Open(cfg.DbPath);
    var hasFts = await db.ExecuteScalarAsync<long>(
        "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='im_fts'");

    if (hasFts == 1)
    {
        var rows = await db.QueryAsync(
            @"SELECT c.item_id,c.title,c.content,c.version
              FROM im_fts f
              JOIN im_items_current c ON c.ns=f.ns AND c.item_id=f.item_id
              WHERE f.ns=@ns AND c.is_active=1 AND im_fts MATCH @q
              LIMIT @k", new { ns = cfg.Ns, q, k });
        return Results.Ok(rows);
    }
    else
    {
        var rows = await db.QueryAsync(
            @"SELECT item_id,title,content,version
              FROM im_items_current WHERE ns=@ns AND is_active=1
                AND (title LIKE @p OR content LIKE @p)
              LIMIT @k", new { ns = cfg.Ns, p = "%" + q + "%", k });
        return Results.Ok(rows);
    }
});


// Chat (compile memory + call local LLM; falls back to stub)
app.MapPost("/v1/chat", async (ChatRequest req, AgentConfig cfg, AppSqliteFactory sf, MemoryCompiler mc) =>
{
    if (string.IsNullOrWhiteSpace(req.Prompt))
        return Results.BadRequest(new { error = "prompt required" });

    await using var db = sf.Open(cfg.DbPath);

    // 1) Select relevant rules (deterministic)
    var selected = await mc.SelectRelevantAsync(db, cfg.Ns, req.Prompt, topK: req.TopK ?? 6);

    // 2) Build structured memory JSON
    var memory = mc.BuildMemoryJson(cfg.Ns, selected);
    Console.WriteLine($"[Agent]: {memory}");

    // 3) Compose messages
    var systemPreamble = """
        You are a coding assistant. You MUST obey the Instruction Memory (IM) provided as JSON.
        Cite IM rule ids in square brackets when you rely on them (e.g., [im:http.client@v3]).
        If a user asks "why", summarize the relevant rules and their ids.
        """;

    var memoryJson = JsonSerializer.Serialize(memory);
    var systemMemory = "INSTRUCTIONS_MEMORY_JSON\n" + memoryJson;

    var messages = new object[] {
        new { role = "system", content = systemPreamble },
        new { role = "system", content = systemMemory },
        new { role = "user",   content = req.Prompt }
    };

    // 4) Call local LLM if configured; else stub
    object llmResponse;
    if (!string.IsNullOrWhiteSpace(cfg.LlmUrl))
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(90) };
        var payload = new { model = req.Model ?? "llama3", messages, temperature = req.Temperature ?? 0.2 };
        var httpResp = await http.PostAsJsonAsync(cfg.LlmUrl, payload);
        var body = await httpResp.Content.ReadAsStringAsync();
        llmResponse = new { ok = httpResp.IsSuccessStatusCode, status = (int)httpResp.StatusCode, body };
    }
    else
    {
        // Simple stub: echo + list rules used
        var rulesList = string.Join(", ", selected.Select(s => $"[im:{s.Id}]"));
        var stubText = $"(stub) You asked: \"{req.Prompt}\".\nRelevant rules: {rulesList}\n";
        llmResponse = new { ok = true, status = 200, body = new { choices = new[] { new { messages} } } };
    }

    return Results.Ok(new
    {
        compiled_memory = memory,
        llm_response = llmResponse
    });

});



// React SPA fallback
app.MapFallbackToFile("index.html");

app.Run();

namespace Agent.Container
{
    public record AgentConfig(string AgentName, string Ns, string NatsUrl, string DbPath, string LlmUrl);

    public record ChatRequest(string Prompt, int? TopK, double? Temperature, string? Model);

    public static class Util
    {
        public static string HexSha256(string s)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            return Convert.ToHexString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(s)));
        }
    }
}
