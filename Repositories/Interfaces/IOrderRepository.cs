using Models.Entities;
namespace Repositories.Interfaces;

public interface IOrderRepository
{
    Task Create(Order order);   // บันทึก order ใหม่พร้อม items ลง DB
    Task<Order?> GetByOrderId(int id); // หา order ด้วย id
    Task Update(Order order);
    Task RemoveItems(List<OrderItem> items);
    Task<List<Order>> SearchOrders(string? orderNumber, string? firstName, string? lastName);
    Task<List<Order>> GetByIds(List<int> ids);
    Task UpdateRange(List<Order> orders);
}
