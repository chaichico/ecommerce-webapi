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

    public UsersController(IUserService userService)
    {
        _userService = userService;
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
        catch (Exception ex)
        {
            return BadRequest(new {message = ex.Message});
        }
    }

    [HttpPost("login")] // Post /api/users/login
    public async Task<IActionResult> Login([FromBody] LoginDto dto) //  รับ json จาก request body
    {
        try
        {
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
            return BadRequest(new { message = ex.Message });
        }
    }
}