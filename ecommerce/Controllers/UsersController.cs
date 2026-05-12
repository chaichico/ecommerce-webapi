using Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Models.Dtos.Requests;
using Models.Dtos.Responses;

namespace Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;

    // in line log -> inject ILogger ที่นี่
    public UsersController(IUserService userService, ILogger<UsersController> logger)
    {
        _userService = userService;
        _logger = logger;
    }
    // route POST /api/users/register
    [HttpPost("register")]
    // FromBody รับ JSON จาก request body
    public async Task<IActionResult> Register([FromBody] RegisterUserDto dto)
    {
        // try catch error
        try
        {
            UserResponseDto result = await _userService.RegisterAsync(dto);
            return CreatedAtAction(nameof(Register), result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new {message = "Internal server error"});
        }
    }

    [HttpPost("login")] // Post /api/users/login
    public async Task<IActionResult> Login([FromBody] LoginDto dto) //  รับ json จาก request body
    {
        try
        {
            _logger.LogInformation("Attempting to log in user with email: {Email}", dto.Email);
            _logger.LogDebug("Login DTO: {@LoginDto}", dto);
            LoginResponseDto result = await _userService.LoginAsync(dto);
            return Ok(result);  // Login success
        }
        catch (UnauthorizedAccessException ex)  
        {
            return Unauthorized(new { message = ex.Message });    // Login failed
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while logging in user with email: {Email}", dto.Email);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}