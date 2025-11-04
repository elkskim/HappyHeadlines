FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["NewsletterService/NewsletterService.csproj", "NewsletterService/"]
COPY ["ArticleDatabase/ArticleDatabase.csproj", "ArticleDatabase/"]
COPY ["Monitoring/Monitoring.csproj", "Monitoring/"]
COPY ["SubscriberDatabase/SubscriberDatabase.csproj", "SubscriberDatabase/"]
COPY ["SubscriberService/SubscriberService.csproj", "SubscriberService/"]
RUN dotnet restore "NewsletterService/NewsletterService.csproj"
COPY ["NewsletterService/", "NewsletterService/"]
COPY ["ArticleDatabase/", "ArticleDatabase/"]
COPY ["Monitoring/", "Monitoring/"]
COPY ["SubscriberDatabase/", "SubscriberDatabase/"]
COPY ["SubscriberService/", "SubscriberService/"]
WORKDIR "/src/NewsletterService"
RUN dotnet build "./NewsletterService.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN rm -f /src/SubscriberService/appsettings.json /src/SubscriberService/appsettings.Development.json \
           /src/Monitoring/appsettings.json
RUN dotnet publish "./NewsletterService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "NewsletterService.dll"]
