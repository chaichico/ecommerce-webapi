using Microsoft.EntityFrameworkCore;
using Data;
using Models;
using Models.Enums;
using Services.Interfaces;

namespace Ecommerce.Tests.Helpers;

public static class TestDataSeeder
{
    public static async Task<User> CreateUserAsync(AppDbContext context, string email = "test@example.com", string firstName = "Test", string lastName = "User")
    {
        User user = new User
        {
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            PasswordHash = "hashedpassword123",
            PhoneNumber = null
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }

    public static async Task<Product> CreateProductAsync(AppDbContext context, string name = "Test Product", decimal price = 100.00m, bool isActive = true)
    {
        Product product = new Product
        {
            ProductName = name,
            Description = "Test description",
            Price = price,
            Stock = 50,
            IsActive = isActive
        };
        context.Products.Add(product);
        await context.SaveChangesAsync();
        return product;
    }

    public static async Task<Order> CreateOrderAsync(AppDbContext context, int userId, OrderStatus status = OrderStatus.Pending)
    {
        Order order = new Order
        {
            OrderNumber = $"ORD-TEST-{Guid.NewGuid().ToString()[..8].ToUpper()}",
            OrderDate = DateTime.UtcNow,
            Status = status,
            ShippingAddress = string.Empty,
            UserId = userId,
            TotalPrice = 0
        };
        context.Orders.Add(order);
        await context.SaveChangesAsync();
        return order;
    }

    public static async Task<Order> CreateOrderWithItemsAsync(AppDbContext context, int userId, Product product, int quantity = 2)
    {
        OrderItem item = new OrderItem
        {
            ProductId = product.Id,
            ProductName = product.ProductName,
            Quantity = quantity,
            UnitPrice = product.Price
        };

        Order order = new Order
        {
            OrderNumber = $"ORD-TEST-{Guid.NewGuid().ToString()[..8].ToUpper()}",
            OrderDate = DateTime.UtcNow,
            Status = OrderStatus.Pending,
            ShippingAddress = string.Empty,
            UserId = userId,
            Items = new List<OrderItem> { item },
            TotalPrice = item.UnitPrice * quantity
        };

        context.Orders.Add(order);
        await context.SaveChangesAsync();
        return order;
    }

    public static async Task<List<User>> SeedDefaultUsersAsync(AppDbContext context, IPasswordHasher passwordHasher, IEncryptionService encryptionService)
    {
        await DbSeeder.SeedAsync(context, passwordHasher, encryptionService);
        return await context.Users.OrderBy(user => user.Id).ToListAsync();
    }
}
