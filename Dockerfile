FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/FrenchInvoice.Core/FrenchInvoice.Core.csproj src/FrenchInvoice.Core/
COPY src/FrenchInvoice.Community/FrenchInvoice.Community.csproj src/FrenchInvoice.Community/
RUN dotnet restore src/FrenchInvoice.Community/FrenchInvoice.Community.csproj

COPY src/ src/
RUN dotnet publish src/FrenchInvoice.Community/FrenchInvoice.Community.csproj -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
VOLUME /app/Data
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "FrenchInvoice.Community.dll"]
