using Models;
namespace Repositories.Interfaces;

public interface IOrderRepository
{
    Task<Order> CreateAsync(Order order);   // บันทึก order ใหม่พร้อม items ลง DB
    Task<Order?> GetByOrderNumberAsync(string orderNumber); // หา order ด้วย order number
}
