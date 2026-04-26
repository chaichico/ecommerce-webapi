using Models.Dtos;
namespace Services.Interfaces;

public interface IOrderService
{
    Task<OrderResponseDto> CreateOrderAsync(CreateOrderDto dto, string userEmail);
    Task<OrderResponseDto> UpdateOrderAsync(int id, UpdateOrderDto dtor, string userEmail);
    Task<OrderResponseDto> ConfirmOrderAsync(int id, ConfirmOrderDto dto, string userEmail);
    Task<List<AdminOrderResponseDto>> SearchOrdersAsync(string? orderNumber, string? firstName, string? lastName);
    Task<List<AdminOrderResponseDto>> ApproveOrdersAsync(ApproveOrdersDto dto);
}