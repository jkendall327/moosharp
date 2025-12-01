# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["MooSharp.sln", "./"]
COPY ["MooSharp/MooSharp.csproj", "MooSharp/"]
COPY ["MooSharp.Web/MooSharp.Web.csproj", "MooSharp.Web/"]
COPY ["MooSharp.Tests/MooSharp.Tests.csproj", "MooSharp.Tests/"]

# Restore dependencies
RUN dotnet restore

# Copy the rest of the source code
COPY . .

# Build and Publish the Web project
WORKDIR "/src/MooSharp.Web"
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080

# Copy the build artifacts
COPY --from=build /app/publish .

# Create a directory for the persistent data
RUN mkdir -p /data

# Set environment variables to point the app to the persistent data folder
ENV AppOptions__DatabaseFilepath=/data/moo.db
ENV AppOptions__WorldDataFilepath=/app/world.json
ENV AppOptions__AgentIdentitiesPath=/app/agents.json

ENTRYPOINT ["dotnet", "MooSharp.Web.dll"]