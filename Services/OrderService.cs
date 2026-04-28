using Models;
using Models.Dtos;
using Models.Enums;
using Repositories.Interfaces;
using Services.Interfaces;

namespace Services;
public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUserRepository _userRepository;
    private readonly IProductRepository _productRepository;

    // constructor
    public OrderService(
        IOrderRepository orderRepository,
        IUserRepository userRepository,
        IProductRepository productRepository)
    {
        _orderRepository = orderRepository;
        _userRepository = userRepository;
        _productRepository = productRepository;
    }

    // Create order function
    public async Task<OrderResponseDto> CreateOrderAsync(CreateOrderDto dto, string userEmail)
    {
        // 1. ดึง user จาก email  จาก JWT claim
        User user = await GetUserByEmailOrThrowAsync(userEmail);

        // 2. ตรวจและดึง product ทุกตัวจาก DB
        List<int> productIds = dto.Items.Select(i => i.ProductId).ToList();
        List<Product> products = await _productRepository.GetActiveByIdsAsync(productIds);

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
            Status = OrderStatus.Pending,
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
        User user = await GetUserByEmailOrThrowAsync(userEmail);

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
        if (order.Status != OrderStatus.Pending)
        {
            throw new InvalidOperationException("Only pending orders can be updated");
        }

        // 5 check and pull every Products from Database
        List<int> productIds = dto.Items.Select(i => i.ProductId).ToList();
        List<Product> products = await _productRepository.GetActiveByIdsAsync(productIds);

        if (products.Count != productIds.Count)
        {
            throw new Exception("One or more products not found or inactive");
        }

        // 6 delete old items and replace with new one
        await _orderRepository.RemoveItemsAsync(order.Items);

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
        User user = await GetUserByEmailOrThrowAsync(userEmail);

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
        if (order.Status != OrderStatus.Pending)
        {
            throw new InvalidOperationException("Only pending orders can be confirmed");
        }

        // 5. ดึง products ที่เกี่ยวข้องกับ order items
        List<int> productIds = order.Items.Select(i => i.ProductId).Distinct().ToList();
        List<Product> products = await _productRepository.GetByIdsAsync(productIds);

        // 6. ตรวจสอบ stock ว่าเพียงพอก่อน confirm
        foreach (OrderItem item in order.Items)
        {
            Product? product = products.FirstOrDefault(p => p.Id == item.ProductId);
            if (product == null || product.Stock < item.Quantity)
            {
                int available = product?.Stock ?? 0;
                throw new InvalidOperationException(
                    $"Insufficient stock for '{item.ProductName}': required {item.Quantity}, available {available}");
            }
        }

        // 7. หัก stock
        foreach (OrderItem item in order.Items)
        {
            Product product = products.First(p => p.Id == item.ProductId);
            product.Stock -= item.Quantity;
        }

        // 8. อัปเดต ShippingAddress และ Status
        order.ShippingAddress = dto.ShippingAddress;
        order.Status = OrderStatus.Confirmed;

        // 9. บันทึกลง DB
        await _orderRepository.UpdateAsync(order);

        // 10. Return response
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

    public async Task<List<AdminOrderResponseDto>> SearchOrdersAsync(string? orderNumber, string? firstName, string? lastName)
    {
        List<Order> orders = await _orderRepository.SearchOrdersAsync(orderNumber, firstName, lastName);

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

        // 3. ตรวจสอบว่ามี orders ที่ยัง Pending อยู่ (ยังไม่ได้ Confirm โดย user)
        List<int> pendingIds = orders.Where(o => o.Status == OrderStatus.Pending).Select(o => o.Id).ToList();
        if (pendingIds.Count > 0)
        {
            throw new InvalidOperationException(
                $"Orders must be confirmed by user before approval. Pending order IDs: {string.Join(", ", pendingIds)}");
        }

        // 4. เก็บเฉพาะ orders ที่ยัง Confirmed
        List<Order> confirmedOrders = orders.Where(o => o.Status == OrderStatus.Confirmed).ToList();

        // 5. เปลี่ยน Status เป็น Approved (stock ถูกหักแล้วตอน Confirm)
        foreach (Order order in confirmedOrders)
        {
            order.Status = OrderStatus.Approved;
        }

        // 6. บันทึกเฉพาะ orders ที่มีการเปลี่ยนแปลง
        await _orderRepository.UpdateRangeAsync(confirmedOrders);

        // 7. เตรียม response
        List<AdminOrderResponseDto> response = orders.Select(o => new AdminOrderResponseDto
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

        // 8. Return response
        return response;
    }

    private async Task<User> GetUserByEmailOrThrowAsync(string email)
    {
        User? user = await _userRepository.GetByEmailAsync(email);
        if (user == null)
        {
            throw new UnauthorizedAccessException("User not found");
        }

        return user;
    }
}