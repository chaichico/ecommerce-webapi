# 🔹 Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# install EF CLI for migration job container (migrate service uses build stage)
RUN dotnet tool install --global dotnet-ef --version 10.*
ENV PATH="$PATH:/root/.dotnet/tools"

# copy csproj แล้ว restore ก่อน (optimize layer)
COPY ecommerce/ecommerce.csproj ./ecommerce/
RUN dotnet restore ecommerce/ecommerce.csproj

# copy ทั้งโปรเจกต์
COPY ecommerce/ ./ecommerce/
RUN dotnet publish ecommerce/ecommerce.csproj -c Release -o out

# 🔹 Stage 2: Runtime (เบากว่า)
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

COPY --from=build /app/out ./

# expose port
EXPOSE 8080

ENTRYPOINT ["dotnet", "ecommerce.dll"]