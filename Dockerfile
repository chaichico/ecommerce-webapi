# 🔹 Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# copy csproj แล้ว restore ก่อน (optimize layer)
COPY *.csproj ./
RUN dotnet restore

# copy ทั้งโปรเจกต์
COPY . ./
RUN dotnet publish -c Release -o out

# 🔹 Stage 2: Runtime (เบากว่า)
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

COPY --from=build /app/out ./

# expose port
EXPOSE 8080

ENTRYPOINT ["dotnet", "ecommerce.dll"]