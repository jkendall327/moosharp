FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["MooSharp.slnx", "./"]
COPY ["MooSharp/MooSharp.csproj", "MooSharp/"]
COPY ["MooSharp.Data/MooSharp.Data.csproj", "MooSharp.Data/"]
COPY ["MooSharp.Web/MooSharp.Web.csproj", "MooSharp.Web/"]
COPY ["MooSharp.Tests/MooSharp.Tests.csproj", "MooSharp.Tests/"]

RUN dotnet restore

COPY . .

WORKDIR "/src/MooSharp.Web"
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080

COPY --from=build /app/publish .

# Create a directory for the persistent data
RUN mkdir -p /data

# Set environment variables to point the app to the persistent data folder
ENV AppOptions__DatabaseFilepath=/data/moo.db
ENV AppOptions__WorldDataFilepath=/app/world.json
ENV AppOptions__AgentIdentitiesPath=/app/agents.json

ENTRYPOINT ["dotnet", "MooSharp.Web.dll"]