# syntax=docker/dockerfile:1

# ---------- build ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Corporate SSL inspection: trust the CA certificates from ./certs (*.crt, PEM) if any.
COPY certs/ /tmp/certs/
RUN if ls /tmp/certs/*.crt >/dev/null 2>&1; then \
      cp /tmp/certs/*.crt /usr/local/share/ca-certificates/ && update-ca-certificates; \
    fi

# Restore first so NuGet packages are cached between builds.
COPY Directory.Build.props ./
COPY src/ReleaseManagement.Domain/ReleaseManagement.Domain.csproj                 src/ReleaseManagement.Domain/
COPY src/ReleaseManagement.Application/ReleaseManagement.Application.csproj       src/ReleaseManagement.Application/
COPY src/ReleaseManagement.Infrastructure/ReleaseManagement.Infrastructure.csproj src/ReleaseManagement.Infrastructure/
COPY src/ReleaseManagement.Web/ReleaseManagement.Web.csproj                       src/ReleaseManagement.Web/
RUN dotnet restore src/ReleaseManagement.Web/ReleaseManagement.Web.csproj

COPY src/ src/
RUN dotnet publish src/ReleaseManagement.Web/ReleaseManagement.Web.csproj \
    -c Release -o /app/publish --no-restore

# ---------- runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# libldap is required by System.DirectoryServices.Protocols (AD login over LDAP on Linux).
# Package name differs between Debian releases, so try both.
RUN apt-get update \
    && (apt-get install -y --no-install-recommends libldap2 || apt-get install -y --no-install-recommends libldap-2.5-0) \
    && rm -rf /var/lib/apt/lists/*

# Same corporate CA certificates for runtime (LDAPS, DevOps Server, PostgreSQL over TLS).
COPY certs/ /tmp/certs/
RUN if ls /tmp/certs/*.crt >/dev/null 2>&1; then \
      cp /tmp/certs/*.crt /usr/local/share/ca-certificates/ && update-ca-certificates; \
    fi \
    && rm -rf /tmp/certs

COPY --from=build /app/publish .

# Writable dirs for attachments, logs and ASP.NET Data Protection keys
# (mount volumes here in docker-compose / Kubernetes so they survive restarts).
RUN mkdir -p /app/App_Data/attachments /app/logs /app/keys

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true

EXPOSE 8080
ENTRYPOINT ["dotnet", "ReleaseManagement.Web.dll"]
