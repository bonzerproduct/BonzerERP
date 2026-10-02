# Build: docker build -t bonzer-api .
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore first (cached layer): only project + central package files.
COPY Directory.Build.props Directory.Packages.props ./
COPY src/Bonzer.Domain/Bonzer.Domain.csproj src/Bonzer.Domain/
COPY src/Bonzer.Application/Bonzer.Application.csproj src/Bonzer.Application/
COPY src/Bonzer.Infrastructure/Bonzer.Infrastructure.csproj src/Bonzer.Infrastructure/
COPY src/Bonzer.Api/Bonzer.Api.csproj src/Bonzer.Api/
RUN dotnet restore src/Bonzer.Api/Bonzer.Api.csproj

COPY src/ src/
RUN dotnet publish src/Bonzer.Api/Bonzer.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=build /app .
RUN mkdir -p /app/logs && chown app /app/logs
USER app
ENTRYPOINT ["dotnet", "Bonzer.Api.dll"]
