FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/LongJobProcessor.API/LongJobProcessor.API.csproj", "src/LongJobProcessor.API/"]
COPY ["src/LongJobProcessor.Domain/LongJobProcessor.Domain.csproj", "src/LongJobProcessor.Domain/"]
COPY ["src/LongJobProcessor.Infrastructure/LongJobProcessor.Infrastructure.csproj", "src/LongJobProcessor.Infrastructure/"]
COPY ["src/LongJobProcessor.Application/LongJobProcessor.Application.csproj", "src/LongJobProcessor.Application/"]
COPY ["src/LongJobProcessor.Persistence/LongJobProcessor.Persistence.csproj", "src/LongJobProcessor.Persistence/"]
RUN dotnet restore "./src/LongJobProcessor.API/LongJobProcessor.API.csproj"
COPY . .
WORKDIR "/src/src/LongJobProcessor.API"
RUN dotnet build "./LongJobProcessor.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./LongJobProcessor.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENV USERNAME="user"
ENV PASSWORD="password"
ENV DefaultConnection="Host=ep-curly-frog-agaxag8s-pooler.c-2.eu-central-1.aws.neon.tech;Database=neondb;Username=neondb_owner;Password=***;SSL Mode=Require;Channel Binding=Require"

ENTRYPOINT ["dotnet", "LongJobProcessor.API.dll"]