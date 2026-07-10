using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineLearningPlatformApi.Application.IServices;
using OnlineLearningPlatformApi.Application.Requests.User;
using OnlineLearningPlatformApi.Application.Responses;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        return ToActionResult(await _authService.LoginAsync(request));
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromForm] RegisterRequest request)
    {
        return ToActionResult(await _authService.RegisterAsync(request));
    }

    [Authorize]
    [HttpGet("profile")]
    public async Task<IActionResult> Profile()
    {
        return ToActionResult(await _authService.ProfileAsync());
    }

    private ObjectResult ToActionResult(ApiResponse response)
    {
        return StatusCode((int)response.StatusCode, response);
    }
}
