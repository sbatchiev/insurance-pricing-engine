FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY NuGet.Config ./
COPY PricingEngine.slnx ./
COPY src/PricingEngine.Api/PricingEngine.Api.csproj src/PricingEngine.Api/
COPY src/PricingEngine.Application/PricingEngine.Application.csproj src/PricingEngine.Application/
COPY src/PricingEngine.Domain/PricingEngine.Domain.csproj src/PricingEngine.Domain/
COPY src/PricingEngine.Infrastructure/PricingEngine.Infrastructure.csproj src/PricingEngine.Infrastructure/
RUN dotnet restore src/PricingEngine.Api/PricingEngine.Api.csproj --configfile NuGet.Config

COPY src/ src/
RUN dotnet publish src/PricingEngine.Api/PricingEngine.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "PricingEngine.Api.dll"]
