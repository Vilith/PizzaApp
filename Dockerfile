FROM mcr.microsoft.com/dotnet/sdk:10.0.401 AS build
WORKDIR /src

COPY global.json ./
COPY PizzaApp.Shared/PizzaApp.Shared.csproj PizzaApp.Shared/
COPY PizzaApp/PizzaApp.csproj PizzaApp/
COPY PizzaApp.Api/PizzaApp.Api.csproj PizzaApp.Api/
RUN dotnet restore PizzaApp.Api/PizzaApp.Api.csproj

COPY PizzaApp.Shared/ PizzaApp.Shared/
COPY PizzaApp/ PizzaApp/
COPY PizzaApp.Api/ PizzaApp.Api/
RUN dotnet publish PizzaApp.Api/PizzaApp.Api.csproj -c Release --no-restore -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_HTTP_PORTS=10000
EXPOSE 10000
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "PizzaApp.Api.dll"]
