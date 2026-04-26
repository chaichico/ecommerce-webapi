using Models;
namespace Repositories.Interfaces;

public interface IOrderRepository
{
    Task<Order> CreateAsync(Order order);   // บันทึก order ใหม่พร้อม items ลง DB
    Task<Order?> GetByOrderIdAsync(int id); // หา order ด้วย id
    Task<Order> UpdateAsync(Order order);
    Task<List<Order>> SearchOrdersAsync(string? orderNumber);
    Task<List<Order>> GetByIdsAsync(List<int> ids);
    Task UpdateRangeAsync(List<Order> orders);
}
