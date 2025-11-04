# Get Base Image (Full .NET Core SDK)
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore
COPY ["ArticleService/ArticleService.csproj", "ArticleService/"]
COPY ["ArticleDatabase/ArticleDatabase.csproj", "ArticleDatabase/"]
COPY ["Monitoring/Monitoring.csproj", "Monitoring/"]
RUN dotnet restore "ArticleService/ArticleService.csproj"

# Copy only required projects
COPY ["ArticleService/", "ArticleService/"]
COPY ["ArticleDatabase/", "ArticleDatabase/"]
COPY ["Monitoring/", "Monitoring/"]
WORKDIR "/src/ArticleService"
RUN dotnet build "ArticleService.csproj" -c Release -o /app/build

# Publish stage
RUN dotnet publish "ArticleService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Generate runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ArticleService.dll", "migrate"]