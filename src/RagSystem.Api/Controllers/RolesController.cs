using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RagSystem.Application.Abstractions;

namespace RagSystem.Api.Controllers;

[ApiController]
[Route("api/roles")]
[Authorize]
public class RolesController : ControllerBase
{
    private readonly IUserRepository _users;
    public RolesController(IUserRepository users) => _users = users;

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) =>
        Ok(await _users.GetAllRolesAsync(ct));
}
