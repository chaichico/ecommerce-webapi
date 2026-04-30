using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Models.Dtos.Requests;
using Models.Dtos.Responses;
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

    // GET /api/orders/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        string? userEmail = User.FindFirst(ClaimTypes.Email)?.Value
                            ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userEmail))
        {
            return Unauthorized(new { message = "Invalid token" });
        }

        try
        {
            OrderResponseDto result = await _orderService.GetOrderByIdAsync(id, userEmail);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (System.Security.SecurityException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
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
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
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

    // PUT /api/orders/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateOrder(int id, [FromBody] UpdateOrderDto dto)
    {
        string? userEmail = User.FindFirst(ClaimTypes.Email)?.Value
                            ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userEmail))
            return Unauthorized(new {message = "Invalid token"});

        try
        {
            OrderResponseDto result = await _orderService.UpdateOrderAsync(id, dto, userEmail);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });  // 404
        }
        catch (System.Security.SecurityException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
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

    // POST /api/orders/{id}/confirm
    [HttpPost("{id}/confirm")]
    public async Task<IActionResult> ConfirmOrder(int id, [FromBody] ConfirmOrderDto dto)
    {
        string? userEmail = User.FindFirst(ClaimTypes.Email)?.Value
                            ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userEmail))
            return Unauthorized(new { message = "Invalid token" });

        try
        {
            OrderResponseDto result = await _orderService.ConfirmOrderAsync(id, dto, userEmail);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (System.Security.SecurityException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
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