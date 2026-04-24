using Data;
using Microsoft.EntityFrameworkCore;
using Models;
using Models.Dtos;
using Repositories.Interfaces;
using Services.Interfaces;

namespace Services;
public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly AppDbContext _context;

    // constructor
    public OrderService(IOrderRepository orderRepository, AppDbContext context)
    {
        _orderRepository = orderRepository;
        _context = context;
    }

    // Create order function
    public async Task<OrderResponseDto> CreateOrderAsync(CreateOrderDto dto, string userEmail)
    {
        // 1. ดึง user จาก email  จาก JWT claim
        User? user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == userEmail);
        if (user == null)
        {
            throw new UnauthorizedAccessException("User not found");
        }

        // 2. ตรวจและดึง product ทุกตัวจาก DB
        List<int> productIds = dto.Items.Select(i => i.ProductId).ToList();
        List<Product> products = await _context.Products
            .Where(p => productIds.Contains(p.Id) && p.IsActive)
            .ToListAsync();

        if (products.Count != productIds.Count)
        {
            throw new Exception("One or more products not found or inactive");
        }

        // 3. create ordernumber 
        string orderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}";

        // 4. create orderitem พร้อม unitprice จาก product จริง
        List<OrderItem> orderItems = dto.Items.Select(item =>
        {
            Product product = products.First(p => p.Id == item.ProductId);
            return new OrderItem
            {
                ProductId = product.Id,
                ProductName = product.ProductName,
                Quantity = item.Quantity,
                UnitPrice = product.Price
            };
        }).ToList();

        // 5. calculate Total Price
        decimal totalPrice = orderItems.Sum(i => i.UnitPrice * i.Quantity);

        // 6. create order entity
        Order order = new Order
        {
            OrderNumber = orderNumber,
            OrderDate = DateTime.UtcNow,
            Status = "Pending",
            ShippingAddress = string.Empty,
            UserId = user.Id,
            Items = orderItems,
            TotalPrice = totalPrice
        };

        // 7. save to database
        await _orderRepository.CreateAsync(order);

        // 8. return response
        return new OrderResponseDto
        {
            OrderNumber = order.OrderNumber,
            OrderDate = order.OrderDate,
            Status = order.Status,
            TotalPrice = order.TotalPrice,
            Items = orderItems.Select(i => new OrderItemResponseDto
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                SubTotal = i.SubTotal
            }).ToList()
        };
    }
}

// - `userEmail` มาจาก JWT claim — ไม่รับจาก request body
// - ตรวจสอบว่า product มีอยู่จริงและ `IsActive = true` ก่อนสร้าง order
// - `UnitPrice` ดึงจาก product จริงใน DB (ป้องกัน user ส่งราคาเองมา)
// - `ShippingAddress` เป็น empty ก่อน — จะกรอกตอน Confirm Order
// - `OrderNumber` สร้างแบบ unique ด้วย format `ORD-{date}-{guid}`