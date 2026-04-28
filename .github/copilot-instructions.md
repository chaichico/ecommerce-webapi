# Project Guidelines

## Tech Stack
- .NET 10 Web API
- Entity Framework Core (Code First)
- SQL Server (configurable via `appsettings.json`)
- JWT Bearer Authentication

## Architecture

Layered architecture with strict dependency direction:

```
Controller → Service → Repository → DbContext
```

- **Controllers** — HTTP concerns only: routing, auth, request/response mapping
- **Services** — Business logic, orchestration
- **Repositories** — Data access via EF Core
- **Models** — EF Core entities
- **Dtos** — Input/output contracts (never expose entities directly)
- **Interfaces** — All services and repositories must have a corresponding interface in `Interfaces/`

## RESTful API Conventions

- Route prefix: `api/[controller]` — controller name must be plural noun (e.g., `OrdersController` → `/api/orders`)
- Use standard HTTP verbs:
  - `GET` — read
  - `POST` — create
  - `PUT` — full update
  - `PATCH` — partial update
  - `DELETE` — delete
- Return appropriate HTTP status codes:
  - `200 OK` — successful GET / PUT / PATCH
  - `201 Created` — successful POST (include created resource in body)
  - `204 No Content` — successful DELETE
  - `400 Bad Request` — validation failure
  - `401 Unauthorized` — missing or invalid token
  - `403 Forbidden` — insufficient permission
  - `404 Not Found` — resource not found
- Response body must always be a typed DTO, never an anonymous object or entity
- Error responses must use a consistent shape: `{ "message": "..." }`

## Code Style

### Explicit Data Types
- **Never** use `var` — always declare explicit types
- **Never** use `dynamic`
- Return types on methods must be explicit (including `Task<T>`)
- Nullable reference types must use `?` annotation explicitly

```csharp
// ✅ Correct
User? user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
List<OrderResponseDto> results = await _orderRepository.GetAllAsync();

// ❌ Wrong
var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
```

### Async/Await
- All I/O operations (DB queries, HTTP calls) must be `async Task<T>`
- Never use `.Result` or `.Wait()`

### Dependency Injection
- Register all services and repositories as `Scoped` in `Program.cs`
- Always inject via interface, never concrete type

```csharp
// ✅ Correct
private readonly IOrderService _orderService;

// ❌ Wrong
private readonly OrderService _orderService;
```

### Naming
- Interfaces: `I` prefix — `IOrderService`, `IUserRepository`
- DTOs: suffix `Dto` — `CreateOrderDto`, `OrderResponseDto`
- Async methods: suffix `Async` — `GetOrderAsync`, `CreateOrderAsync`
- Private fields: `_camelCase`

## DTOs

- Use separate DTOs for input (`CreateXDto`, `UpdateXDto`) and output (`XResponseDto`)
- Never return EF Core entities from controllers
- Place all DTOs under `Models/Dtos/`

## EF Core

- Use Code First migrations — never modify the database manually
- Entity configurations go in `Data/Configurations/` using `IEntityTypeConfiguration<T>`
- Seed data is handled in `Data/DbSeeder.cs` and called at startup only when collection is empty

## Authentication

- JWT Bearer for user-facing endpoints — extract claims via `User.FindFirst(...)`
- Basic Authentication for admin endpoints — credentials sourced from environment variables only, never hardcoded
- Sensitive fields (phone number) use symmetric encryption via `IEncryptionService`
- Passwords use one-way hashing via `IPasswordHasher`

## Configuration

- All configurable values (connection strings, JWT secret, admin credentials) must be in `appsettings.json` / environment variables
- Never hardcode secrets in source code
