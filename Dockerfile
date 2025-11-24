# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY IdleMonitorServer.csproj .
RUN dotnet restore IdleMonitorServer.csproj

COPY . .
RUN dotnet publish IdleMonitorServer.csproj -c Release -o /app

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 5000
ENTRYPOINT ["dotnet", "IdleMonitorServer.dll"]
