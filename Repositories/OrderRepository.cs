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

    public async Task<Order?> GetByOrderIdAsync(int id)
    {
        return await _context.Orders
            .Include(o => o.Items) // รวม OrderItems ด้วย
            .Include(o => o.User)  // รวม User ด้วย
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<Order> UpdateAsync(Order order)
    {
        _context.Orders.Update(order); // EF core track changes ของ Order แลพ items
        await _context.SaveChangesAsync();
        return order;
    }
}