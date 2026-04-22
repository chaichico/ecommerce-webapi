using Models;
using Microsoft.EntityFrameworkCore;

namespace Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        // ตรวจสอบว่ามีข้อมูลอยู่แล้วหรือไม่
        if (await context.Users.AnyAsync() || await context.Products.AnyAsync())
        {
            return; // มีข้อมูลอยู่แล้ว ไม่ต้อง seed
        }

        // Seed Users
        var users = new List<User>
        {
            new User
            {
                Email = "john.doe@example.com",
                FirstName = "John",
                LastName = "Doe",
                PasswordHash = "hashed_password_1",
                PhoneNumber = "081-234-5678"
            },
            new User
            {
                Email = "jane.smith@example.com",
                FirstName = "Jane",
                LastName = "Smith",
                PasswordHash = "hashed_password_2",
                PhoneNumber = "082-345-6789"
            },
            new User
            {
                Email = "somchai.thai@example.com",
                FirstName = "สมชาย",
                LastName = "ใจดี",
                PasswordHash = "hashed_password_3",
                PhoneNumber = "083-456-7890"
            }
        };

        await context.Users.AddRangeAsync(users);
        await context.SaveChangesAsync();

        // Seed Products
        var products = new List<Product>
        {
            new Product
            {
                ProductName = "Laptop Dell XPS 15",
                Description = "High-performance laptop with 16GB RAM and 512GB SSD",
                Price = 45000.00m,
                Stock = 10,
                IsActive = true
            },
            new Product
            {
                ProductName = "iPhone 15 Pro",
                Description = "Latest iPhone with A17 Pro chip",
                Price = 42900.00m,
                Stock = 25,
                IsActive = true
            },
            new Product
            {
                ProductName = "Samsung Galaxy S24",
                Description = "Flagship Android phone with AI features",
                Price = 32900.00m,
                Stock = 15,
                IsActive = true
            },
            new Product
            {
                ProductName = "Sony WH-1000XM5",
                Description = "Premium noise-cancelling headphones",
                Price = 13900.00m,
                Stock = 30,
                IsActive = true
            },
            new Product
            {
                ProductName = "iPad Air",
                Description = "Powerful tablet with M1 chip",
                Price = 24900.00m,
                Stock = 20,
                IsActive = true
            }
        };

        await context.Products.AddRangeAsync(products);
        await context.SaveChangesAsync();

        // Seed Orders
        var orders = new List<Order>
        {
            new Order
            {
                OrderNumber = "ORD-2026-001",
                OrderDate = DateTime.UtcNow.AddDays(-5),
                Status = "Completed",
                ShippingAddress = "123 ถนนสุขุมวิท กรุงเทพฯ 10110",
                UserId = users[0].Id,
                TotalPrice = 58900.00m
            },
            new Order
            {
                OrderNumber = "ORD-2026-002",
                OrderDate = DateTime.UtcNow.AddDays(-3),
                Status = "Shipping",
                ShippingAddress = "456 ถนนพระราม 4 กรุงเทพฯ 10330",
                UserId = users[1].Id,
                TotalPrice = 45000.00m
            },
            new Order
            {
                OrderNumber = "ORD-2026-003",
                OrderDate = DateTime.UtcNow.AddDays(-1),
                Status = "Pending",
                ShippingAddress = "789 ถนนเพชรบุรี กรุงเทพฯ 10400",
                UserId = users[2].Id,
                TotalPrice = 37800.00m
            }
        };

        await context.Orders.AddRangeAsync(orders);
        await context.SaveChangesAsync();

        // Seed OrderItems
        var orderItems = new List<OrderItem>
        {
            // Order 1 items
            new OrderItem
            {
                OrderId = orders[0].Id,
                ProductId = products[1].Id.ToString(),
                ProductName = products[1].ProductName,
                Quantity = 1,
                UnitPrice = products[1].Price
            },
            new OrderItem
            {
                OrderId = orders[0].Id,
                ProductId = products[3].Id.ToString(),
                ProductName = products[3].ProductName,
                Quantity = 1,
                UnitPrice = products[3].Price
            },
            // Order 2 items
            new OrderItem
            {
                OrderId = orders[1].Id,
                ProductId = products[0].Id.ToString(),
                ProductName = products[0].ProductName,
                Quantity = 1,
                UnitPrice = products[0].Price
            },
            // Order 3 items
            new OrderItem
            {
                OrderId = orders[2].Id,
                ProductId = products[4].Id.ToString(),
                ProductName = products[4].ProductName,
                Quantity = 1,
                UnitPrice = products[4].Price
            },
            new OrderItem
            {
                OrderId = orders[2].Id,
                ProductId = products[3].Id.ToString(),
                ProductName = products[3].ProductName,
                Quantity = 1,
                UnitPrice = products[3].Price
            }
        };

        await context.OrderItems.AddRangeAsync(orderItems);
        await context.SaveChangesAsync();
    }
}
