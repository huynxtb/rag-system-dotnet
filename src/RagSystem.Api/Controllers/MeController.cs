using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RagSystem.Application.Abstractions;

namespace RagSystem.Api.Controllers;

[ApiController]
[Route("api/me")]
[Authorize]
public class MeController : ControllerBase
{
    private readonly ICurrentUser _user;
    public MeController(ICurrentUser user) => _user = user;

    [HttpGet]
    public IActionResult Get() => Ok(new { id = _user.Id, email = _user.Email, roles = _user.Roles });
}
