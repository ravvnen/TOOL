using Microsoft.AspNetCore.Mvc;
using Modules.MemoryManagement;

namespace Modules.AgentOperations;

[ApiController]
[Route("agent/{ns}")]
public sealed class AgentController : ControllerBase
{
    private readonly MemoryCompiler _memoryCompiler;
    private readonly ILogger<AgentController> _log;

    public AgentController(MemoryCompiler memoryCompiler, ILogger<AgentController> log)
    {
        _memoryCompiler = memoryCompiler;
        _log = log;
    }

    /// <summary>
    /// Retrieve relevant rules for LLM context based on user prompt.
    /// Replaces distributed agent database queries with centralized memory.
    /// </summary>
    [HttpGet("memory")]
    public async Task<IActionResult> GetMemory(
        [FromRoute] string ns,
        [FromQuery] string? q = null,
        [FromQuery] int k = 10
    )
    {
        try
        {
            if (string.IsNullOrWhiteSpace(ns))
            {
                return BadRequest(new { error = "namespace (ns) is required" });
            }

            if (k <= 0 || k > 100)
            {
                return BadRequest(new { error = "k must be between 1 and 100" });
            }

            var prompt = q ?? "";
            var items = await _memoryCompiler.SelectRelevantAsync(ns, prompt, k);
            var response = _memoryCompiler.BuildMemoryJson(ns, items);

            _log.LogInformation(
                "Memory query: ns={Ns} prompt='{Prompt}' returned {Count} items",
                ns,
                prompt,
                items.Count
            );

            return Ok(response);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error retrieving memory for ns={Ns}", ns);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Health check endpoint for agent containers to verify TOOL connectivity.
    /// </summary>
    [HttpGet("health")]
    public IActionResult Health([FromRoute] string ns)
    {
        return Ok(
            new
            {
                status = "healthy",
                ns,
                timestamp = DateTimeOffset.UtcNow.ToString("o"),
                service = "TOOL-centralized",
            }
        );
    }

    /// <summary>
    /// Chat endpoint - process user prompt with LLM using relevant memory context.
    /// </summary>
    [HttpPost("chat")]
    public async Task<IActionResult> Chat([FromRoute] string ns, [FromBody] ChatRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Prompt))
            {
                return BadRequest(new { error = "prompt is required" });
            }

            // 1) Get relevant memory/rules
            var items = await _memoryCompiler.SelectRelevantAsync(
                ns,
                request.Prompt,
                request.TopK ?? 6
            );
            var memory = _memoryCompiler.BuildMemoryJson(ns, items);

            // 2) Build LLM messages with memory context
            var systemPreamble = """
                You are a coding assistant. You MUST obey the Instruction Memory (IM) provided as JSON.
                Cite IM rule ids in square brackets when you rely on them (e.g., [im:http.client@v3]).
                If a user asks "why", summarize the relevant rules and their ids.
                """;

            var memoryJson = System.Text.Json.JsonSerializer.Serialize(memory);
            var systemMemory = "INSTRUCTIONS_MEMORY_JSON\n" + memoryJson;

            var messages = new object[]
            {
                new { role = "system", content = systemPreamble },
                new { role = "system", content = systemMemory },
                new { role = "user", content = request.Prompt },
            };

            // 3) Return memory + messages (let frontend handle LLM call)
            return Ok(
                new
                {
                    ns,
                    prompt = request.Prompt,
                    memory,
                    messages,
                    llm_ready = true,
                }
            );
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error processing chat for ns={Ns}", ns);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Debug: List all items/rules in the namespace for debugging.
    /// </summary>
    [HttpGet("debug/items")]
    public async Task<IActionResult> GetDebugItems(
        [FromRoute] string ns,
        [FromQuery] int limit = 50
    )
    {
        try
        {
            var items = await _memoryCompiler.SelectRelevantAsync(ns, "", limit);

            return Ok(
                new
                {
                    ns,
                    count = items.Count,
                    items = items.Select(item => new
                    {
                        id = item.Id,
                        title = item.Title,
                        content = item.Content.Length > 300
                            ? item.Content[..300] + "..."
                            : item.Content,
                        source = item.Source,
                    }),
                }
            );
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error retrieving debug items for ns={Ns}", ns);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Agent state summary.
    /// </summary>
    [HttpGet("state")]
    public async Task<IActionResult> GetState([FromRoute] string ns)
    {
        try
        {
            var items = await _memoryCompiler.SelectRelevantAsync(ns, "", 1000);
            var activeCount = items.Count;

            // Create a hash of all content for state verification
            var allContent = string.Join("\n---\n", items.Select(i => i.Title + "\n" + i.Content));
            var hash = Convert.ToHexString(
                System.Security.Cryptography.SHA256.HashData(
                    System.Text.Encoding.UTF8.GetBytes(allContent)
                )
            );

            return Ok(
                new
                {
                    ns,
                    active_items = activeCount,
                    content_hash = hash[..16], // First 16 chars
                    last_updated = DateTimeOffset.UtcNow.ToString("o"),
                }
            );
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error retrieving state for ns={Ns}", ns);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}

// DTOs for Agent API
public record ChatRequest(
    string Prompt,
    int? TopK = null,
    double? Temperature = null,
    string? Model = null
);
