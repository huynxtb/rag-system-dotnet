using Microsoft.AspNetCore.Mvc;
using RagSystem.Application.Auth;

namespace RagSystem.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _auth;
    public AuthController(AuthService auth) => _auth = auth;

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req, CancellationToken ct)
    {
        var result = await _auth.LoginAsync(req, ct);
        if (result is null) return Unauthorized(new { error = "Invalid credentials" });
        return Ok(result);
    }
}
