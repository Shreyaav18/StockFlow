FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["StockFlow.Web/StockFlow.Web.csproj", "StockFlow.Web/"]
RUN dotnet restore "StockFlow.Web/StockFlow.Web.csproj"
COPY . .
WORKDIR "/src/StockFlow.Web"
RUN dotnet publish "StockFlow.Web.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
# Do NOT set ASPNETCORE_URLS here — docker-compose.yml sets ASPNETCORE_HTTP_PORTS=8080
# which is the preferred way in .NET 8+. Having both causes the "Overriding" warning.
ENTRYPOINT ["dotnet", "StockFlow.Web.dll"]