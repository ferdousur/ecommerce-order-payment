# Stage 1: Build & Publish Stage

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy .csproj files first to leverage Docker layer caching
COPY ["ECommerce.Api/ECommerce.Api.csproj", "ECommerce.Api/"]
COPY ["ECommerce.Application/ECommerce.Application.csproj", "ECommerce.Application/"]
COPY ["ECommerce.Domain/ECommerce.Domain.csproj", "ECommerce.Domain/"]
COPY ["ECommerce.Infrastructure/ECommerce.Infrastructure.csproj", "ECommerce.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "ECommerce.Api/ECommerce.Api.csproj"

# Copy the rest of the source code
COPY . .

# Build and Publish in Release mode
WORKDIR "/src/ECommerce.Api"
RUN dotnet publish "ECommerce.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Final Runtime Stage

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS final
WORKDIR /app

# Expose ports (HTTP & HTTPS)
EXPOSE 8080
EXPOSE 8081

# Copy published output from build stage
COPY --from=build /app/publish .

# Set environment variable
ENV ASPNETCORE_URLS=http://+:8080

# Entry point to run the API
ENTRYPOINT ["dotnet", "ECommerce.Api.dll"]