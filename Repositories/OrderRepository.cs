using Data;
using Microsoft.EntityFrameworkCore;
using Models.Entities;
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

    public async Task RemoveItemsAsync(List<OrderItem> items)
    {
        _context.Set<OrderItem>().RemoveRange(items);
        await _context.SaveChangesAsync();
    }

    public async Task<List<Order>> SearchOrdersAsync(string? orderNumber, string? firstName, string? lastName)
    {
        IQueryable<Order> query = _context.Orders
            .Include(o => o.Items)
            .Include(o => o.User);

        if (!string.IsNullOrWhiteSpace(orderNumber))
            query = query.Where(o => o.OrderNumber.Contains(orderNumber));

        if (!string.IsNullOrWhiteSpace(firstName))
            query = query.Where(o => o.User.FirstName.Contains(firstName));

        if (!string.IsNullOrWhiteSpace(lastName))
            query = query.Where(o => o.User.LastName.Contains(lastName));

        return await query.ToListAsync();
    }

    public async Task<List<Order>> GetByIdsAsync(List<int> ids)
    {
        return await _context.Orders
            .Include(o => o.Items)
            .Include(o => o.User)
            .Where(o => ids.Contains(o.Id))
            .ToListAsync();
    }

    public async Task UpdateRangeAsync(List<Order> orders)
    {
        _context.Orders.UpdateRange(orders);
        await _context.SaveChangesAsync();
    }
}