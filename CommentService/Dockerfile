# Get Base Image (Full .NET Core SDK)
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore
COPY ["CommentService/CommentService.csproj", "CommentService/"]
COPY ["CommentDatabase/CommentDatabase.csproj", "CommentDatabase/"]
RUN dotnet restore "CommentService/CommentService.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/CommentService"
RUN dotnet build "CommentService.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "CommentService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Generate runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CommentService.dll"]