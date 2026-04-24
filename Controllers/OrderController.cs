using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Models.Dtos;
using Services.Interfaces;

namespace Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    // POST /api/orders
    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto dto)
    {
        // ดึง email จาก JWT claim
        string? userEmail = User.FindFirst(ClaimTypes.Email)?.Value
                            ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userEmail))
        {
            return Unauthorized(new {message = "Invalid token"});
        }

        try
        {
            OrderResponseDto result = await _orderService.CreateOrderAsync(dto, userEmail);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new {message = ex.Message});
        }
        catch (Exception ex)
        {
            return BadRequest(new {message = ex.Message});
        }
    }
}