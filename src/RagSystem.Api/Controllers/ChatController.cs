using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RagSystem.Application.Chats;
using RagSystem.Application.Rag;

namespace RagSystem.Api.Controllers;

[ApiController]
[Route("api/chat")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly RagService _rag;
    private readonly ChatSessionService _sessions;

    public ChatController(RagService rag, ChatSessionService sessions)
    {
        _rag = rag;
        _sessions = sessions;
    }

    // --- Stateless one-shot Q&A (kept for backwards compat) ---
    [HttpPost("ask")]
    public async Task<IActionResult> Ask([FromBody] AskRequest req, CancellationToken ct) =>
        Ok(await _rag.AskAsync(req, ct));

    // --- Session-scoped endpoints ---
    [HttpGet("sessions")]
    public async Task<IActionResult> ListSessions(CancellationToken ct) =>
        Ok(await _sessions.ListMineAsync(ct));

    [HttpPost("sessions")]
    public async Task<IActionResult> CreateSession(CancellationToken ct) =>
        Ok(await _sessions.CreateAsync(ct));

    [HttpGet("sessions/{id}")]
    public async Task<IActionResult> GetSession(string id, CancellationToken ct)
    {
        var s = await _sessions.GetAsync(id, ct);
        if (s is null) return NotFound();
        return Ok(s);
    }

    [HttpDelete("sessions/{id}")]
    public async Task<IActionResult> DeleteSession(string id, CancellationToken ct)
    {
        await _sessions.DeleteAsync(id, ct);
        return NoContent();
    }

    [HttpPost("sessions/{id}/messages")]
    public async Task<IActionResult> PostMessage(string id, [FromBody] AskInSessionRequest req, CancellationToken ct) =>
        Ok(await _rag.AskInSessionAsync(id, req, ct));
}
