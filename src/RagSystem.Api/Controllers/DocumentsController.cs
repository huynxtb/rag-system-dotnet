using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RagSystem.Application.Documents;

namespace RagSystem.Api.Controllers;

[ApiController]
[Route("api/documents")]
[Authorize]
public class DocumentsController : ControllerBase
{
    private readonly DocumentService _svc;
    public DocumentsController(DocumentService svc) => _svc = svc;

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) =>
        Ok(await _svc.ListVisibleAsync(ct));

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id, CancellationToken ct)
    {
        var doc = await _svc.GetAsync(id, ct);
        if (doc is null) return NotFound();
        return Ok(doc);
    }

    [HttpPost("upload")]
    [RequestSizeLimit(100_000_000)]
    public async Task<IActionResult> Upload(
        [FromForm] IFormFile file,
        [FromForm] string type,
        [FromForm] string allowedRoles, // comma-separated
        CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "File is required" });

        var roles = (allowedRoles ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        await using var stream = file.OpenReadStream();
        var result = await _svc.UploadAsync(
            new UploadDocumentRequest(stream, file.FileName, type, roles), ct);
        return Ok(result);
    }
}
