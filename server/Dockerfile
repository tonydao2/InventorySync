# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Copy project file and restore dependencies
COPY InventorySync.csproj ./
RUN dotnet restore

# Copy all source and publish
COPY . ./
RUN dotnet publish -c Release -o out

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Set environment variable (this makes Kestrel listen on port 5001)
ENV ASPNETCORE_URLS=http://+:5001

EXPOSE 5001

COPY --from=build /app/out .
ENTRYPOINT ["dotnet", "InventorySync.dll"]
