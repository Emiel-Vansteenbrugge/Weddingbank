FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/backend/Directory.Build.props src/backend/Directory.Build.props
COPY src/backend/WeddingBank.Domain/WeddingBank.Domain.csproj src/backend/WeddingBank.Domain/
COPY src/backend/WeddingBank.Application/WeddingBank.Application.csproj src/backend/WeddingBank.Application/
COPY src/backend/WeddingBank.Infrastructure/WeddingBank.Infrastructure.csproj src/backend/WeddingBank.Infrastructure/
COPY src/backend/WeddingBank.Api/WeddingBank.Api.csproj src/backend/WeddingBank.Api/

RUN dotnet restore src/backend/WeddingBank.Api/WeddingBank.Api.csproj

COPY src/backend/WeddingBank.Domain/ src/backend/WeddingBank.Domain/
COPY src/backend/WeddingBank.Application/ src/backend/WeddingBank.Application/
COPY src/backend/WeddingBank.Infrastructure/ src/backend/WeddingBank.Infrastructure/
COPY src/backend/WeddingBank.Api/ src/backend/WeddingBank.Api/

RUN dotnet publish src/backend/WeddingBank.Api/WeddingBank.Api.csproj --no-restore -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "WeddingBank.Api.dll"]
