using Models.Dtos;
namespace Services.Interfaces;

public interface IOrderService
{
    Task<OrderResponseDto> CreateOrderAsync(CreateOrderDto dto, string userEmail);
    Task<OrderResponseDto> UpdateOrderAsync(int id, UpdateOrderDto dtor, string userEmail);
}