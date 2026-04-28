## 📂 โครงสร้างโปรเจกต์

```
ecommerce/
├── Controllers/
│   ├── AdminController.cs
│   ├── OrdersController.cs
│   └── UsersController.cs
├── Data/
│   ├── AppDbContext.cs
│   ├── DbSeeder.cs
│   └── Configurations/
│       ├── OrderConfiguration.cs
│       ├── OrderItemConfiguration.cs
│       ├── ProductConfiguration.cs
│       └── UserConfiguration.cs
├── Models/
│   ├── Dtos/
│   │   ├── AdminOrderResponseDto.cs
│   │   ├── ApproveOrdersDto.cs
│   │   ├── ConfirmOrderDto.cs
│   │   ├── CreateOrderDto.cs
│   │   ├── CreateOrderItemDto.cs
│   │   ├── LoginDto.cs
│   │   ├── LoginResponseDto.cs
│   │   ├── OrderResponseDto.cs
│   │   ├── RegisterUserDto.cs
│   │   ├── UpdateOrderDto.cs
│   │   └── UserResponseDto.cs
│   ├── Enums/
│   │   └── OrderStatus.cs
│   ├── Order.cs
│   ├── OrderItem.cs
│   ├── Product.cs
│   └── User.cs
├── Repositories/
│   ├── Interfaces/
│   │   ├── IOrderRepository.cs
│   │   ├── IProductRepository.cs
│   │   └── IUserRepository.cs
│   ├── OrderRepository.cs
│   ├── ProductRepository.cs
│   └── UserRepository.cs
├── Services/
│   ├── Interfaces/
│   │   ├── IEncryptionService.cs
│   │   ├── IOrderService.cs
│   │   ├── IPasswordHasher.cs
│   │   └── IUserService.cs
│   ├── EncryptionService.cs
│   ├── OrderService.cs
│   ├── PasswordHasher.cs
│   └── UserService.cs
├── Migrations/
├── docs/
├── Ecommerce.Tests/
├── Program.cs
├── appsettings.json
├── appsettings.Development.json
├── appsettings.Test.json
├── ecommerce.csproj
├── ecommerce.sln
├── docker-compose.yml
└── Dockerfile
```

## 1 Run Database
docker compose up -d sqlserver

# 2 update database (apply migration)
dotnet ef database update

## 3 Test
dotnet test

## 4 Run API
dotnet run

## SwaggerUI Go to this link
http://localhost:8080/swagger



### หาก run โดยใช้คำสั่ง
docker compose up -d --build
หาก container api ไม่ run ให้รอ sqlserver พร้อม
จากนั้นให้ run container api ใหม่


<!-- If no migration file or incase you need to-->
# สร้าง migration ใหม่
dotnet ef migrations add Init

# ลบ migration ล่าสุด
dotnet ef migrations remove

# ดู migration ทั้งหมด
dotnet ef migrations list