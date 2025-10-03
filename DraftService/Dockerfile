# Get Base Image (Full .NET Core SDK)
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore
COPY ["DraftService/DraftService.csproj", "DraftService/"]
COPY ["../DraftDatabase/DraftDatabase.csproj", "DraftDatabase/"]
RUN dotnet restore "DraftService/DraftService.csproj"

# Copy everything else and build
COPY . .
COPY ../DraftDatabase ../DraftDatabase
WORKDIR "/src/DraftService"
RUN dotnet build "DraftService.csproj" -c Release -o /app/build

# Publish stage
RUN dotnet publish "DraftService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Generate runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .
COPY DraftService/appsettings.json ./appsettings.json
ENTRYPOINT ["dotnet", "DraftService.dll", "migrate"]