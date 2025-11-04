FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["PublisherService/PublisherService.csproj", "PublisherService/"]
COPY ["ArticleDatabase/ArticleDatabase.csproj", "ArticleDatabase/"]
COPY ["Monitoring/Monitoring.csproj", "Monitoring/"]
RUN dotnet restore "PublisherService/PublisherService.csproj"
COPY ["PublisherService/", "PublisherService/"]
COPY ["ArticleDatabase/", "ArticleDatabase/"]
COPY ["Monitoring/", "Monitoring/"]
WORKDIR "/src/PublisherService"
RUN dotnet build "./PublisherService.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./PublisherService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PublisherService.dll"]
