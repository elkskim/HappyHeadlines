# Get Base Image (Full .NET Core SDK)
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore
COPY ["CommentService/CommentService.csproj", "CommentService/"]
COPY ["CommentDatabase/CommentDatabase.csproj", "CommentDatabase/"]
COPY ["Monitoring/Monitoring.csproj", "Monitoring/"]
COPY ["ProfanityService/ProfanityService.csproj", "ProfanityService/"]
COPY ["ProfanityDatabase/ProfanityDatabase.csproj", "ProfanityDatabase/"]
RUN dotnet restore "CommentService/CommentService.csproj"

# Copy only required projects
COPY ["CommentService/", "CommentService/"]
COPY ["CommentDatabase/", "CommentDatabase/"]
COPY ["Monitoring/", "Monitoring/"]
COPY ["ProfanityService/", "ProfanityService/"]
COPY ["ProfanityDatabase/", "ProfanityDatabase/"]
WORKDIR "/src/CommentService"
RUN dotnet build "CommentService.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN rm -f /src/ProfanityService/appsettings.json /src/ProfanityService/appsettings.Development.json \
           /src/Monitoring/appsettings.json
RUN dotnet publish "CommentService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Generate runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CommentService.dll"]