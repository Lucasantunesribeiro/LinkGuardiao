# Imagem da API para desenvolvimento local / docker compose.
# Em producao a API roda como AWS Lambda (ver scripts/aws e infra/cdk); esta imagem nao e usada no deploy.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Directory.Packages.props ./
COPY src/LinkGuardiao.Application/LinkGuardiao.Application.csproj src/LinkGuardiao.Application/
COPY src/LinkGuardiao.Infrastructure/LinkGuardiao.Infrastructure.csproj src/LinkGuardiao.Infrastructure/
COPY src/LinkGuardiao.Infrastructure.PostgreSQL/LinkGuardiao.Infrastructure.PostgreSQL.csproj src/LinkGuardiao.Infrastructure.PostgreSQL/
COPY src/LinkGuardiao.Api/LinkGuardiao.Api.csproj src/LinkGuardiao.Api/
RUN dotnet restore src/LinkGuardiao.Api/LinkGuardiao.Api.csproj

COPY src/ src/
RUN dotnet publish src/LinkGuardiao.Api/LinkGuardiao.Api.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true
COPY --from=build /app/publish ./
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "LinkGuardiao.Api.dll"]
