## 📂 โครงสร้างโปรเจกต์

```
ecommerce/
├── .github/
│   └── copilot-instructions.md
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
├── docs/
│   ├── architecture/
│   │   └── spec.md
│   ├── guides/
│   │   ├── api-checklist.md
│   │   ├── checklist.md
│   │   ├── db_setup.md
│   │   └── last-refactor-checklist.md
│   ├── tasks/
│   │   └── ...
│   ├── code-review-1-thai.md
│   ├── code-review-1.md
│   ├── code-review-2.md
│   ├── last-review.md
│   └── review-restfulAPI.md
├── Models/
│   ├── Dtos/
│   │   ├── Requests/
│   │   │   ├── ApproveOrdersDto.cs
│   │   │   ├── ConfirmOrderDto.cs
│   │   │   ├── CreateOrderDto.cs
│   │   │   ├── CreateOrderItemDto.cs
│   │   │   ├── LoginDto.cs
│   │   │   ├── RegisterUserDto.cs
│   │   │   └── UpdateOrderDto.cs
│   │   └── Responses/
│   │       ├── AdminOrderResponseDto.cs
│   │       ├── LoginResponseDto.cs
│   │       ├── OrderResponseDto.cs
│   │       └── UserResponseDto.cs
│   ├── Entities/
│   │   ├── Order.cs
│   │   ├── OrderItem.cs
│   │   ├── Product.cs
│   │   └── User.cs
│   ├── Enums/
│   │   └── OrderStatus.cs
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
├── Ecommerce.Tests/
│   ├── Fakes/
│   ├── Helpers/
│   ├── Repositories/
│   ├── Services/
│   ├── UnitTest1.cs
│   └── Ecommerce.Tests.csproj
├── Migrations/
│   ├── 20260417090318_InitialCreate.cs
│   ├── 20260417090318_InitialCreate.Designer.cs
│   ├── 20260423060405_FixOrderItemProductId.cs
│   ├── 20260423060405_FixOrderItemProductId.Designer.cs
│   ├── 20260428041451_SyncLatestModel.cs
│   ├── 20260428041451_SyncLatestModel.Designer.cs
│   ├── 20260428082235_ConvertOrderStatusToEnum.cs
│   ├── 20260428082235_ConvertOrderStatusToEnum.Designer.cs
│   └── AppDbContextModelSnapshot.cs
├── Properties/
│   └── launchSettings.json
├── Program.cs
├── ecommerce.http
├── appsettings.json
├── appsettings.Development.json
├── appsettings.Test.json
├── build_output.txt
├── ecommerce.csproj
├── ecommerce.sln
├── docker-compose.yml
└── Dockerfile
```
# RUN WEB API แบบ LOCAL 
## 1 Run Database
docker compose up -d db

## 2 update database (apply migration)
dotnet ef database update

## 3 Test
dotnet test

## 4 Run API
dotnet run

## SwaggerUI Go to this link
http://localhost:8080/swagger



# RUN WEB API แบบ Container
docker compose up -d --build



<!-- If no migration file or incase you need to-->
## สร้าง migration ใหม่
dotnet ef migrations add Init

## ลบ migration ล่าสุด
dotnet ef migrations remove

## ดู migration ทั้งหมด
dotnet ef migrations list