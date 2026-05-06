# New Project Structure

วัตถุประสงค์:
- เพิ่มโฟลเดอร์ Ecommerce สำหรับเก็บไฟล์และโฟลเดอร์ที่เป็น source code
- ให้ root ของโปรเจกต์เหลือไฟล์ที่เกี่ยวกับ config, tooling และเอกสาร

## โครงสร้างที่แนะนำ

/
  .vscode/
  .gitignore
  Dockerfile
  docker-compose.yml
  README.md
  appsettings.json
  appsettings.Development.json
  appsettings.Test.json
  ecommerce.sln
  build_output.txt
  docs/
  Ecommerce/
    ecommerce.csproj
    Program.cs
    Properties/
      launchSettings.json

    Controllers/
      AdminController.cs
      OrdersController.cs
      UsersController.cs

    Data/
      AppDbContext.cs
      DbSeeder.cs
      Configurations/
        OrderConfiguration.cs
        OrderItemConfiguration.cs
        ProductConfiguration.cs
        UserConfiguration.cs

    Models/
      Dtos/
      Entities/
      Enums/

    Repositories/
      Interfaces/
      OrderRepository.cs
      ProductRepository.cs
      UserRepository.cs

    Services/
      Interfaces/
      EncryptionService.cs
      OrderService.cs
      PasswordHasher.cs
      UserService.cs

    Migrations/
      (migration files ทั้งหมด)

    Ecommerce.Tests/
      Ecommerce.Tests.csproj
      Fakes/
      Helpers/
      Repositories/
      Services/
      UnitTest1.cs

## Mapping การย้ายหลักจากโครงสร้างเดิม

- Program.cs -> Ecommerce/Program.cs
- Controllers/ -> Ecommerce/Controllers/
- Data/ -> Ecommerce/Data/
- Models/ -> Ecommerce/Models/
- Repositories/ -> Ecommerce/Repositories/
- Services/ -> Ecommerce/Services/
- Migrations/ -> Ecommerce/Migrations/
- Properties/ -> Ecommerce/Properties/
- ecommerce.csproj -> Ecommerce/ecommerce.csproj
- Ecommerce.Tests/ -> Ecommerce/Ecommerce.Tests/

## หมายเหตุสำคัญหลังย้าย

- อัปเดต path ใน ecommerce.sln ให้ชี้ไปที่ Ecommerce/ecommerce.csproj และ Ecommerce/Ecommerce.Tests/Ecommerce.Tests.csproj
- ถ้ามี Docker build context หรือ COPY path ให้ปรับตามโครงสร้างใหม่
- ตรวจสอบ launch profile และไฟล์ config ที่อ้าง path แบบ relative
- รัน dotnet restore และ dotnet build หลังย้ายเพื่อยืนยันว่าอ้างอิงถูกต้อง
