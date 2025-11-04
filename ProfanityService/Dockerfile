# Get Base Image (Full .NET Core SDK)
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore
COPY ["ProfanityService/ProfanityService.csproj", "ProfanityService/"]
COPY ["ProfanityDatabase/ProfanityDatabase.csproj", "ProfanityDatabase/"]
COPY ["Monitoring/Monitoring.csproj", "Monitoring/"]
RUN dotnet restore "ProfanityService/ProfanityService.csproj"

# Copy only required projects
COPY ["ProfanityService/", "ProfanityService/"]
COPY ["ProfanityDatabase/", "ProfanityDatabase/"]
COPY ["Monitoring/", "Monitoring/"]
WORKDIR "/src/ProfanityService"
RUN dotnet build "ProfanityService.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "ProfanityService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Generate runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ProfanityService.dll"]