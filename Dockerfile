# Etapa base
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8089

# Etapa de build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["NotificationDelivery.csproj", "./"]
RUN dotnet restore "./NotificationDelivery.csproj"
COPY . .

RUN dotnet build "NotificationDelivery.csproj" -c Release -o /app/build
RUN dotnet publish "NotificationDelivery.csproj" -c Release -o /app/publish

# Etapa final
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD curl --fail http://localhost:8089/health || exit 1

ENTRYPOINT ["dotnet", "NotificationDelivery.dll"]
