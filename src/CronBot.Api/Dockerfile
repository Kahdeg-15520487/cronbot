# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["CronBot.sln", "./"]
COPY ["Directory.Build.props", "./"]
COPY ["global.json", "./"]
COPY ["src/CronBot.Api/CronBot.Api.csproj", "src/CronBot.Api/"]
COPY ["src/CronBot.Application/CronBot.Application.csproj", "src/CronBot.Application/"]
COPY ["src/CronBot.Domain/CronBot.Domain.csproj", "src/CronBot.Domain/"]
COPY ["src/CronBot.Infrastructure/CronBot.Infrastructure.csproj", "src/CronBot.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "src/CronBot.Api/CronBot.Api.csproj"

# Copy source code
COPY ["src/CronBot.Api/", "src/CronBot.Api/"]
COPY ["src/CronBot.Application/", "src/CronBot.Application/"]
COPY ["src/CronBot.Domain/", "src/CronBot.Domain/"]
COPY ["src/CronBot.Infrastructure/", "src/CronBot.Infrastructure/"]

# Build
WORKDIR "/src/src/CronBot.Api"
RUN dotnet build "CronBot.Api.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "CronBot.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*

EXPOSE 8080
EXPOSE 8081

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CronBot.Api.dll"]
