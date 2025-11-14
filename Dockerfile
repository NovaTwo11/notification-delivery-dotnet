# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["NotificationDelivery.csproj", "."]
RUN dotnet restore "NotificationDelivery.csproj"
COPY . .
RUN dotnet publish "NotificationDelivery.csproj" -c Release -o /app/publish

# Stage 2: Final
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8089
ENV ASPNETCORE_URLS=http://+:8080

FROM base AS final
WORKDIR /app
#
# --- LA LÍNEA CORREGIDA ESTÁ AQUÍ ---
# Antes decía --from=publish, ahora dice --from=build
#
COPY --from=build /app/publish .

HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD curl --fail http://localhost:8089/health || exit 1

ENTRYPOINT ["dotnet", "NotificationDelivery.dll"]