# Bygger InnoviaHub.Api (med DataAccess och Shared) för DigitalOcean App Platform

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Kopiera projektfilerna först så att restore cachas mellan byggen
COPY InnoviaHub.Api/InnoviaHub.Api.csproj InnoviaHub.Api/
COPY InnoviaHub.DataAccess/InnoviaHub.DataAccess.csproj InnoviaHub.DataAccess/
COPY InnoviaHub.Shared/InnoviaHub.Shared.csproj InnoviaHub.Shared/
RUN dotnet restore InnoviaHub.Api/InnoviaHub.Api.csproj

COPY InnoviaHub.Api/ InnoviaHub.Api/
COPY InnoviaHub.DataAccess/ InnoviaHub.DataAccess/
COPY InnoviaHub.Shared/ InnoviaHub.Shared/
RUN dotnet publish InnoviaHub.Api/InnoviaHub.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

# .NET-imagen lyssnar på port 8080 som standard
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "InnoviaHub.Api.dll"]
