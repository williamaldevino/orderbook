FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["OrderBookWorker/OrderBookWorker.csproj", "OrderBookWorker/"]
COPY ["OrderBookCore/OrderBookCore.csproj", "OrderBookCore/"]
RUN dotnet restore "OrderBookWorker/OrderBookWorker.csproj"
COPY . .
WORKDIR "/src/OrderBookWorker"
RUN dotnet build "./OrderBookWorker.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./OrderBookWorker.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "OrderBookWorker.dll"]
