# Multi-stage Dockerfile for .NET 10 ASP.NET Core
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["Read_It.csproj", "./"]
RUN dotnet restore "./Read_It.csproj"

# Copy the rest of the source code and publish
COPY . .
RUN dotnet publish "Read_It.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Create uploads directory
RUN mkdir -p /app/wwwroot/uploads/notes

# Default Render port is 10000
ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000

ENTRYPOINT ["dotnet", "Read_It.dll"]
