using Microsoft.AspNetCore.Mvc;

namespace Modules.Replay;

[ApiController]
[Route("api/v1/admin")]
public class ReplayController : ControllerBase
{
    private readonly ReplayEngine _replayEngine;
    private readonly ILogger<ReplayController> _log;

    public ReplayController(ReplayEngine replayEngine, ILogger<ReplayController> log)
    {
        _replayEngine = replayEngine;
        _log = log;
    }

    /// <summary>
    /// Trigger a replay operation (synchronous, for testing/admin)
    /// </summary>
    [HttpPost("replay")]
    [ProducesResponseType(typeof(ReplayResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> TriggerReplay([FromBody] ReplayRequest request)
    {
        // namespace to replay, default if not provided
        var ns = request.Ns ?? "ravvnen.consulting";

        // Create temporary database for replay
        var tempDbPath = Path.Combine(Path.GetTempPath(), $"replay_{Guid.NewGuid():N}.db");

        try
        {
            _log.LogInformation("Triggering replay for ns={Ns}, outputDb={DbPath}", ns, tempDbPath);

            var result = await _replayEngine.ReplayAsync(
                ns,
                tempDbPath,
                request.MaxSequence,
                HttpContext.RequestAborted
            );

            // Clean up temporary database after successful replay
            if (System.IO.File.Exists(tempDbPath))
            {
                System.IO.File.Delete(tempDbPath);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Replay failed for ns={Ns}", ns);

            // Clean up temporary database on error
            if (System.IO.File.Exists(tempDbPath))
            {
                try
                {
                    System.IO.File.Delete(tempDbPath);
                }
                catch
                { /* ignore cleanup errors */
                }
            }

            return StatusCode(500, new { error = "Replay failed", detail = ex.Message });
        }
    }
}
