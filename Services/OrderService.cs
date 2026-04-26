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

    public async Task<OrderResponseDto> UpdateOrderAsync(int id, UpdateOrderDto dto, string userEmail)
    {
        // 1 find User from email
        User? user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == userEmail);
        if (user == null)
        {
            throw new UnauthorizedAccessException("User not found");
        }

        // 2 find User from Id and Include Items
        Order? order = await _orderRepository.GetByOrderIdAsync(id);
        if (order == null)
        {
            throw new KeyNotFoundException("Order not found");
        }

        // 3 check if User is the owner of this Order
        if (order.UserId != user.Id)
        {
            throw new System.Security.SecurityException("You are not authorized to update this order");
        }
        // 4 check if order is still Pending
        if (order.Status != "Pending")
        {
            throw new InvalidOperationException("Only pending orders can be updated");
        }

        // 5 check and pull every Products from Database
        List<int> productIds = dto.Items.Select(i => i.ProductId).ToList();
        List<Product> products = await _context.Products
            .Where(p => productIds.Contains(p.Id) && p.IsActive)
            .ToListAsync();

        if (products.Count != productIds.Count)
        {
            throw new Exception("One or more products not found or inactive");
        }

        // 6 delete old items and replace with new one
        _context.Set<OrderItem>().RemoveRange(order.Items);

        List<OrderItem> newItems = dto.Items.Select(item =>
        {
            Product product = products.First(p => p.Id == item.ProductId);
            return new OrderItem
            {
                OrderId = order.Id,
                ProductId = product.Id,
                ProductName = product.ProductName,
                Quantity = item.Quantity,
                UnitPrice = product.Price
            };
        }).ToList();

        // 7 calculate TotalPrice again
        decimal totalPrice = newItems.Sum(i => i.UnitPrice * i.Quantity);

        // 8 update order entity
        order.Items = newItems;
        order.TotalPrice = totalPrice;

        // 9 save changes to database
        await _orderRepository.UpdateAsync(order);

        // 10 return response
        return new OrderResponseDto
        {
            OrderNumber = order.OrderNumber,
            OrderDate = order.OrderDate,
            Status = order.Status,
            TotalPrice = order.TotalPrice,
            Items = newItems.Select(i => new OrderItemResponseDto
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                SubTotal = i.SubTotal
            }).ToList()
        };

    }

    public async Task<OrderResponseDto> ConfirmOrderAsync(int id, ConfirmOrderDto dto, string userEmail)
    {
        // 1. หา user จาก email (ดึงมาจาก JWT claim)
        User? user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == userEmail);
        if (user == null)
        {
            throw new UnauthorizedAccessException("User not found");
        }

        // 2. หา order จาก id พร้อม Include Items
        Order? order = await _orderRepository.GetByOrderIdAsync(id);
        if (order == null)
        {
            throw new KeyNotFoundException("Order not found");
        }

        // 3. ตรวจสอบว่า order เป็นของ user คนนี้
        if (order.UserId != user.Id)
        {
            throw new System.Security.SecurityException("You are not authorized to confirm this order");
        }

        // 4. ตรวจสอบว่า order ยัง Pending อยู่
        if (order.Status != "Pending")
        {
            throw new InvalidOperationException("Only pending orders can be confirmed");
        }

        // 5. อัปเดต ShippingAddress และ Status
        order.ShippingAddress = dto.ShippingAddress;
        order.Status = "Confirmed";

        // 6. บันทึกลง DB
        await _orderRepository.UpdateAsync(order);

        // 7. Return response
        return new OrderResponseDto
        {
            OrderNumber = order.OrderNumber,
            OrderDate = order.OrderDate,
            Status = order.Status,
            TotalPrice = order.TotalPrice,
            Items = order.Items.Select(i => new OrderItemResponseDto
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                SubTotal = i.SubTotal
            }).ToList()
        };
    }

    public async Task<List<AdminOrderResponseDto>> SearchOrdersAsync(string? orderNumber)
    {
        List<Order> orders = await _orderRepository.SearchOrdersAsync(orderNumber);

        return orders.Select(o => new AdminOrderResponseDto
        {
            OrderNumber = o.OrderNumber,
            OrderDate = o.OrderDate,
            Status = o.Status,
            TotalPrice = o.TotalPrice,
            ShippingAddress = o.ShippingAddress,
            User = new AdminUserInfoDto
            {
                FirstName = o.User.FirstName,
                LastName = o.User.LastName,
                Email = o.User.Email
            },
            Items = o.Items.Select(i => new OrderItemResponseDto
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                SubTotal = i.SubTotal
            }).ToList()
        }).ToList();
    }

    public async Task<List<AdminOrderResponseDto>> ApproveOrdersAsync(ApproveOrdersDto dto)
    {
        // 1. ดึง orders ตาม OrderIds ที่ส่งมา
        List<Order> orders = await _orderRepository.GetByIdsAsync(dto.OrderIds);

        // 2. ตรวจสอบว่าพบ orders ทั้งหมดไหม
        if (orders.Count != dto.OrderIds.Count)
        {
            List<int> foundIds = orders.Select(o => o.Id).ToList();
            List<int> notFound = dto.OrderIds.Except(foundIds).ToList();
            throw new KeyNotFoundException($"Orders not found: {string.Join(", ", notFound)}");
        }

        // 3. เปลี่ยน Status เป็น Confirmed เฉพาะที่ยัง Pending อยู่
        foreach (Order order in orders)
        {
            if (order.Status == "Pending")
            {
                order.Status = "Confirmed";
            }
        }

        // 4. บันทึกทั้งหมดใน batch เดียว
        await _orderRepository.UpdateRangeAsync(orders);

        // 5. Return response
        return orders.Select(o => new AdminOrderResponseDto
        {
            OrderNumber = o.OrderNumber,
            OrderDate = o.OrderDate,
            Status = o.Status,
            TotalPrice = o.TotalPrice,
            ShippingAddress = o.ShippingAddress,
            User = new AdminUserInfoDto
            {
                FirstName = o.User.FirstName,
                LastName = o.User.LastName,
                Email = o.User.Email
            },
            Items = o.Items.Select(i => new OrderItemResponseDto
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                SubTotal = i.SubTotal
            }).ToList()
        }).ToList();
    }
}

// - `userEmail` มาจาก JWT claim — ไม่รับจาก request body
// - ตรวจสอบว่า product มีอยู่จริงและ `IsActive = true` ก่อนสร้าง order
// - `UnitPrice` ดึงจาก product จริงใน DB (ป้องกัน user ส่งราคาเองมา)
// - `ShippingAddress` เป็น empty ก่อน — จะกรอกตอน Confirm Order
// - `OrderNumber` สร้างแบบ unique ด้วย format `ORD-{date}-{guid}`