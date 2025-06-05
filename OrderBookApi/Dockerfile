FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["OrderBookApi/OrderBookApi.csproj", "OrderBookApi/"]
COPY ["OrderBookCore/OrderBookCore.csproj", "OrderBookCore/"]
RUN dotnet restore "OrderBookApi/OrderBookApi.csproj"
COPY . .
WORKDIR "/src/OrderBookApi"
RUN dotnet build "./OrderBookApi.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./OrderBookApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "OrderBookApi.dll"]
