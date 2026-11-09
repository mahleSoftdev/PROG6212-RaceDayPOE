# syntax=docker/dockerfile:1

# ---- build stage: restore first so package layers are cached, then publish ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY RaceDayPOE.Web.csproj ./
RUN dotnet restore RaceDayPOE.Web.csproj
COPY . .
RUN dotnet publish RaceDayPOE.Web.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ---- runtime stage: small ASP.NET image, no SDK, non-root user ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_EnableDiagnostics=0
COPY --from=build /app/publish .
# The base image ships a non-root "app" user (UID 1654).
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "RaceDayPOE.Web.dll"]
