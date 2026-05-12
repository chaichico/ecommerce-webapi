## 📂 โครงสร้างโปรเจกต์

```
ECOMMERCE-PROJECT
├── 📁 docs
│   ├── 📁 architecture
│   ├── 📁 guides
│   └── 📁 tasks
├── 📁 ecommerce
│   ├── ⚙️ appsettings.json
│   ├── ⚙️ appsettings.Development.json
│   ├── 📁 Controllers
│   ├── 📁 Data
│   │   └── 📁 Configurations
│   ├── 📁 Logging
│   ├── 📁 Mappings
│   ├── 📁 Middleware
│   ├── 📁 Migrations
│   ├── 📁 Models
│   │   ├── 📁 Dtos
│   │   │   ├── 📁 Requests
│   │   │   └── 📁 Responses
│   │   ├── 📁 Entities
│   │   └── 📁 Enums
│   ├── 📁 Properties
│   ├── 📁 Repositories
│   │   └── 📁 Interfaces
│   ├── 📁 Services
│   │   └── 📁 Interfaces
│   ├── 📁 logs
│   └── 🐳 Dockerfile
├── 📁 Ecommerce.Tests
│   ├── 📁 Services
│   └── 📄Ecommerce.Tests.csproj
│── 📁 logs
├── 🐳 docker-compose.yml
├── 📄 .env
├── 📄 ecommerce.sln
└── 📄 README.md

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

# เข้าไป Exec ใน Docker
cat /app/logs/audit-20260507.json
cat /app/logs/summary-20260507.json


<!-- If no migration file or incase you need to-->
## สร้าง migration ใหม่
dotnet ef migrations add Init

## ลบ migration ล่าสุด
dotnet ef migrations remove

## ดู migration ทั้งหมด
dotnet ef migrations list

# โหลด .json ลงมาดู
docker cp ecommerce-api:/app/logs/audit-<YYYYMMDD>.json ./audit.json
docker cp ecommerce-api:/app/logs/summary-<YYYYMMDD>.json ./summary.json

# reset logs 
rm ./logs/audit-20260507.json
rm ./logs/summary-20260507.json


