# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["SecretCustomer.API/SecretCustomer.API.csproj", "SecretCustomer.API/"]
COPY ["SecretCustomer.Services/SecretCustomer.Services.csproj", "SecretCustomer.Services/"]
COPY ["SecretCustomer.Data/SecretCustomer.Data.csproj", "SecretCustomer.Data/"]
COPY ["SecretCustomer.Core/SecretCustomer.Core.csproj", "SecretCustomer.Core/"]

RUN dotnet restore "SecretCustomer.API/SecretCustomer.API.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/SecretCustomer.API"
RUN dotnet build "SecretCustomer.API.csproj" -c Release -o /app/build

# Publish Stage
FROM build AS publish
RUN dotnet publish "SecretCustomer.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Copy published app
COPY --from=publish /app/publish .

# Create non-root user
RUN groupadd -r appuser && useradd -r -g appuser appuser
USER appuser

ENTRYPOINT ["dotnet", "SecretCustomer.API.dll"]
