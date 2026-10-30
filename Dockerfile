# 1. BYGGMILJÖ: återställ paket och publicera servern med Blazor-klienten.
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

# 2. KÖRMILJÖ: publicerade filer, HTTP-port och användare utan rootbehörighet.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_HTTP_PORTS=10000
ENV PLAYWRIGHT_BROWSERS_PATH=/ms-playwright
EXPOSE 10000
COPY --from=build /app/publish .
# 3. MENYBILD: publicerade Playwright-filer kan vara körbara enbart för ägaren.
# Installationen körs som root, men driver och Chromium måste kunna köras av appanvändaren.
RUN chmod -R a+rX /app/.playwright \
    && dotnet PizzaApp.Api.dll --install-menu-browser \
    && chmod -R a+rX /ms-playwright
USER $APP_UID
# Kontrollera med samma användare som kör servern: ett behörighetsfel stoppar nu bygget.
RUN /app/.playwright/node/linux-x64/node --version
ENTRYPOINT ["dotnet", "PizzaApp.Api.dll"]
