# Migration Plan: Manual Mapping → AutoMapper

## ภาพรวม

เปลี่ยนการ mapping ใน Service layer จาก manual object initializer เป็น AutoMapper  
เน้นทิศทาง **Entity → DTO (output)** เป็นหลัก เพราะทิศทาง DTO → Entity บางส่วนมี logic พิเศษที่ AutoMapper จัดการไม่ได้

---

## วิเคราะห์ก่อน migrate

### ✅ Mapping ที่ migrate ได้ทั้งหมด (Entity → DTO)

| Source | Destination | หมายเหตุ |
|---|---|---|
| `OrderItem` | `OrderItemResponseDto` | ทุก field ชื่อตรงกัน, `SubTotal` เป็น computed property |
| `Order` | `OrderResponseDto` | field ตรงกัน, `Items` เป็น collection ให้ map nested ด้วย |
| `User` | `UserResponseDto` | 3 field ชื่อตรงกัน |
| `User` | `AdminUserInfoDto` | 3 field ชื่อตรงกัน |
| `Order` | `AdminOrderResponseDto` | field ตรงกัน, nested `User` + `Items` |

### ⚠️ Mapping ที่ migrate ได้บางส่วน (DTO → Entity)

| Source | Destination | ปัญหา |
|---|---|---|
| `RegisterUserDto` | `User` | `Password` ต้องผ่าน hash ก่อนเป็น `PasswordHash`, `PhoneNumber` ต้องผ่าน encrypt |
| `CreateOrderDto` / `UpdateOrderDto` | `Order` + `OrderItem` | `OrderItem` ต้องดึง `ProductName` และ `UnitPrice` จาก DB ไม่ใช่จาก DTO |

**แนวทาง:** mapping สองตัวนี้ให้คงเป็น manual ตามเดิม เพราะถ้า inject service เข้า AutoMapper Profile จะทำให้ architecture ซับซ้อนเกินจำเป็น

---

## ขั้นตอนการ migrate

### Step 1 — ติดตั้ง AutoMapper [x] finished

```bash
dotnet add package AutoMapper
dotnet add package AutoMapper.Extensions.Microsoft.DependencyInjection
```

---

### Step 2 — สร้าง Mapping Profiles [x] finished 

สร้างโฟลเดอร์ `Mappings/` ที่ root ของ project แล้วสร้างไฟล์:

#### `Mappings/OrderProfile.cs`

```csharp
using AutoMapper;
using Models.Dtos.Responses;
using Models.Entities;

namespace Mappings;

public class OrderProfile : Profile
{
    public OrderProfile()
    {
        // OrderItem → OrderItemResponseDto
        // SubTotal เป็น computed property อยู่แล้ว AutoMapper อ่านได้เลย
        CreateMap<OrderItem, OrderItemResponseDto>();

        // Order → OrderResponseDto
        // Items เป็น collection ให้ AutoMapper map nested ให้อัตโนมัติ
        CreateMap<Order, OrderResponseDto>();

        // User → AdminUserInfoDto
        CreateMap<User, AdminUserInfoDto>();

        // Order → AdminOrderResponseDto
        // field User และ Items เป็น nested object ที่ map ต่อไปได้
        CreateMap<Order, AdminOrderResponseDto>()
            .ForMember(dest => dest.User, opt => opt.MapFrom(src => src.User));
    }
}
```

#### `Mappings/UserProfile.cs`

```csharp
using AutoMapper;
using Models.Dtos.Responses;
using Models.Entities;

namespace Mappings;

public class UserProfile : Profile
{
    public UserProfile()
    {
        // User → UserResponseDto (3 fields ตรงกันหมด)
        CreateMap<User, UserResponseDto>();
    }
}
```

---

### Step 3 — Register AutoMapper ใน Program.cs [x] finished

```csharp
// เพิ่มก่อน builder.Build()
builder.Services.AddAutoMapper(typeof(Mappings.OrderProfile).Assembly);
```

---

### Step 4 — Inject IMapper เข้า Services [x] finished

#### `OrderService.cs`

```csharp
using AutoMapper;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUserRepository _userRepository;
    private readonly IProductRepository _productRepository;
    private readonly IMapper _mapper;  // เพิ่ม

    public OrderService(
        IOrderRepository orderRepository,
        IUserRepository userRepository,
        IProductRepository productRepository,
        IMapper mapper)  // เพิ่ม
    {
        _orderRepository = orderRepository;
        _userRepository = userRepository;
        _productRepository = productRepository;
        _mapper = mapper;  // เพิ่ม
    }
    // ...
}
```

#### `UserService.cs`

```csharp
using AutoMapper;

public class UserService : IUserService
{
    // ... existing fields ...
    private readonly IMapper _mapper;  // เพิ่ม

    public UserService(..., IMapper mapper)
    {
        // ... existing assignments ...
        _mapper = mapper;  // เพิ่ม
    }
    // ...
}
```

---

### Step 5 — แทนที่ manual mapping ด้วย `_mapper.Map<>()` [x] finished

#### `OrderService.cs` — ลบ `MapOrderResponse()` แล้วแทนด้วย

```csharp
// เดิม
return MapOrderResponse(order);

// ใหม่
return _mapper.Map<OrderResponseDto>(order);
```

```csharp
// เดิม — SearchOrdersAsync และ ApproveOrdersAsync (inline 2 ที่)
return orders.Select(o => new AdminOrderResponseDto
{
    OrderNumber = o.OrderNumber,
    // ...
}).ToList();

// ใหม่
return _mapper.Map<List<AdminOrderResponseDto>>(orders);
```

#### `UserService.cs`

```csharp
// เดิม — RegisterAsync
return new UserResponseDto
{
    Email = user.Email,
    FirstName = user.FirstName,
    LastName = user.LastName
};

// ใหม่
return _mapper.Map<UserResponseDto>(user);
```

```csharp
// เดิม — LoginAsync
User = new UserResponseDto
{
    Email = user.Email,
    FirstName = user.FirstName,
    LastName = user.LastName
}

// ใหม่
User = _mapper.Map<UserResponseDto>(user)
```

---

## สิ่งที่ **ไม่ต้องเปลี่ยน**

- Logic สร้าง `OrderItem` จาก `CreateOrderDto` / `UpdateOrderDto` (ต้องดึง ProductName + UnitPrice จาก DB)
- Logic สร้าง `User` จาก `RegisterUserDto` (ต้องผ่าน hash + encrypt ก่อน)

---

## Checklist

- [ ] ติดตั้ง NuGet package AutoMapper
- [ ] สร้าง `Mappings/OrderProfile.cs`
- [ ] สร้าง `Mappings/UserProfile.cs`
- [x] Register ใน `Program.cs`
- [x] Inject `IMapper` เข้า `OrderService`
- [x] Inject `IMapper` เข้า `UserService`
- [x] แทนที่ `MapOrderResponse()` ทุกที่ด้วย `_mapper.Map<OrderResponseDto>(order)`
- [x] แทนที่ inline `AdminOrderResponseDto` mapping ใน `SearchOrdersAsync`
- [x] แทนที่ inline `AdminOrderResponseDto` mapping ใน `ApproveOrdersAsync`
- [x] แทนที่ `UserResponseDto` mapping ใน `RegisterAsync`
- [x] แทนที่ `UserResponseDto` mapping ใน `LoginAsync`
- [x] ลบ private method `MapOrderResponse()` ออก
- [x] รัน tests ตรวจสอบว่าไม่มี regression
