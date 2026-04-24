using Data;
using Microsoft.EntityFrameworkCore;
using Models;
using Repositories.Interfaces;

namespace Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly AppDbContext _context;

    public OrderRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Order> CreateAsync(Order order)
    {
        _context.Orders.Add(order);
        await _context.SaveChangesAsync(); // บันทึก order และ orderItems ในครั้งเดียว
        return order; 
    }

    public async Task<Order?> GetByOrderNumberAsync(string orderNumber)
    {
        return await _context.Orders
            .Include(o => o.Items) // รวม OrderItems ด้วย
            .Include(o => o.User)  // รวม User ด้วย
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);
    }
}