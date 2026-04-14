using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RagSystem.Application.Documents;

namespace RagSystem.Api.Controllers;

[ApiController]
[Route("api/document-types")]
[Authorize]
public class DocumentTypesController : ControllerBase
{
    private readonly DocumentTypeService _svc;
    public DocumentTypesController(DocumentTypeService svc) => _svc = svc;

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) =>
        Ok(await _svc.ListAsync(ct));

    public record CreateTypeRequest(string Name, string DisplayName);

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTypeRequest req, CancellationToken ct) =>
        Ok(await _svc.CreateAsync(req.Name, req.DisplayName, ct));
}
