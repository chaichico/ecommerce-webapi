using System.Text;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using Models.Dtos;
using Services.Interfaces;

namespace Controllers;

[ApiController]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly IConfiguration _configuration;

    public AdminController(IOrderService orderService, IConfiguration configuration)
    {
        _orderService = orderService;
        _configuration = configuration;
    }

    // ── Basic Auth helper ──────────────────────────────────────────────────────
    private bool IsAuthorized()
    {
        string? authHeader = Request.Headers["Authorization"].FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Basic "))
            return false;

        string base64Credentials = authHeader["Basic ".Length..].Trim();
        string credentials;
        try
        {
            credentials = Encoding.UTF8.GetString(Convert.FromBase64String(base64Credentials));
        }
        catch (FormatException)
        {
            return false;
        }

        string[] parts = credentials.Split(':', 2);
        if (parts.Length != 2)
            return false;

        string username = _configuration["AdminAuth:Username"] ?? string.Empty;
        string password = _configuration["AdminAuth:Password"] ?? string.Empty;

        bool isUsernameValid = FixedTimeStringEquals(parts[0], username);
        bool isPasswordValid = FixedTimeStringEquals(parts[1], password);
        return isUsernameValid && isPasswordValid;
    }

    private static bool FixedTimeStringEquals(string left, string right)
    {
        byte[] leftHash = SHA256.HashData(Encoding.UTF8.GetBytes(left));
        byte[] rightHash = SHA256.HashData(Encoding.UTF8.GetBytes(right));
        return CryptographicOperations.FixedTimeEquals(leftHash, rightHash);
    }

    // GET /api/admin/orders?orderNumber=&firstName=&lastName=
    [HttpGet("orders")]
    public async Task<IActionResult> SearchOrders(
        [FromQuery] string? orderNumber,
        [FromQuery] string? firstName,
        [FromQuery] string? lastName)
    {
        if (!IsAuthorized())
            return Unauthorized(new { message = "Invalid credentials" });

        try
        {
            List<AdminOrderResponseDto> result = await _orderService.SearchOrdersAsync(orderNumber, firstName, lastName);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // POST /api/admin/orders/approve
    [HttpPost("orders/approve")]
    public async Task<IActionResult> ApproveOrders([FromBody] ApproveOrdersDto dto)
    {
        if (!IsAuthorized())
            return Unauthorized(new { message = "Invalid credentials" });

        try
        {
            List<AdminOrderResponseDto> result = await _orderService.ApproveOrdersAsync(dto);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
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
