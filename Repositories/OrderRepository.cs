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

    public Task Create(Order order)
    {
        _context.Orders.Add(order);
        return _context.SaveChangesAsync(); // บันทึก order และ orderItems ในครั้งเดียว
    }

    public Task<Order?> GetByOrderId(int id)
    {
        return _context.Orders
            .Include(o => o.Items) // รวม OrderItems ด้วย
            .Include(o => o.User)  // รวม User ด้วย
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public Task Update(Order order)
    {
        _context.Orders.Update(order); // EF core track changes ของ Order แลพ items
        return _context.SaveChangesAsync();
    }

    public Task RemoveItems(List<OrderItem> items)
    {
        _context.Set<OrderItem>().RemoveRange(items);
        return _context.SaveChangesAsync();
    }

    public Task<List<Order>> SearchOrders(string? orderNumber, string? firstName, string? lastName)
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

        return query.ToListAsync();
    }

    public Task<List<Order>> GetByIds(List<int> ids)
    {
        return _context.Orders
            .Include(o => o.Items)
            .Include(o => o.User)
            .Where(o => ids.Contains(o.Id))
            .ToListAsync();
    }

    public Task UpdateRange(List<Order> orders)
    {
        _context.Orders.UpdateRange(orders);
        return _context.SaveChangesAsync();
    }
}