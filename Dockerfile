FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY SpotRent.Api/SpotRent.Api.csproj SpotRent.Api/
COPY SpotRent.Services/SpotRent.Services.csproj SpotRent.Services/
COPY SpotRent.Infrastructure/SpotRent.Infrastructure.csproj SpotRent.Infrastructure/
COPY SpotRent.Domain/SpotRent.Domain.csproj SpotRent.Domain/
RUN dotnet restore SpotRent.Api/SpotRent.Api.csproj

COPY SpotRent.Api/. SpotRent.Api/
COPY SpotRent.Services/. SpotRent.Services/
COPY SpotRent.Infrastructure/. SpotRent.Infrastructure/
COPY SpotRent.Domain/. SpotRent.Domain/

WORKDIR /src/SpotRent.Api
RUN dotnet publish SpotRent.Api.csproj -c Release -o /app/publish /p:UseAppHost=false
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "SpotRent.Api.dll"]
