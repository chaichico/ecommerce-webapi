## 📂 โครงสร้างโปรเจกต์

```
ecommerce/
├── Controllers/          # API Controllers
│   └── UsersController.cs
├── Data/                 # Database Context & Configurations
│   ├── AppDbContext.cs
│   └── DbSeeder.cs
├── Interfaces/           # Service Interfaces
│   ├── IPasswordHasher.cs
│   ├── IEncryptionService.cs
│   ├── IUserRepository.cs
│   └── IUserService.cs
├── Models/               # Domain Models & DTOs
│   ├── DTOs/
│   │   ├── RegisterUserDto.cs
│   │   ├── LoginDto.cs
│   │   └── UserResponseDto.cs
│   ├── User.cs
│   ├── Order.cs
│   ├── OrderItem.cs
│   └── Product.cs
├── Repositories/         # Data Access Layer
│   └── UserRepository.cs
├── Services/             # Business Logic Layer
│   ├── PasswordHasher.cs
│   ├── EncryptionService.cs
│   └── UserService.cs
├── Migrations/           # EF Core Migrations
├── docs/                 # Documentation
│   ├── api-registration-step.md
│   ├── api-login-step.md
│   └── db_setup.md
├── Program.cs            # Application Entry Point
├── appsettings.json      # Configuration
├── docker-compose.yml    # Docker Configuration
└── Dockerfile
```

## Run Database
docker compose up -d sqlserver

# update database (apply migration)
dotnet ef database update

## Test
dotnet test

## Run API
dotnet run


# สร้าง migration ใหม่
dotnet ef migrations add Init

# ลบ migration ล่าสุด
dotnet ef migrations remove

# ดู migration ทั้งหมด
dotnet ef migrations list