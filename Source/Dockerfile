# The "non-standalone" CdrAuthServer has gateways. 
# This image seems to only be needed for the "build-for-fapi-testing" pipeline

###############################################################################
# Build base layer
###############################################################################
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base

WORKDIR /app
EXPOSE 8081
EXPOSE 8082
ENV ASPNETCORE_ENVIRONMENT=Release

###############################################################################
# Build CdrAuthServer API layer
###############################################################################
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ./ .

FROM build AS publish
COPY ./Directory.Build.props /app/Directory.Build.props
COPY ./.editorconfig /app/.editorconfig
COPY ./CdrAuthServer.Domain/. /app/CdrAuthServer.Domain
COPY ./CdrAuthServer.Repository/. /app/CdrAuthServer.Repository
COPY ./CdrAuthServer/. /app/CdrAuthServer
COPY ./CdrAuthServer.Infrastructure/. /app/CdrAuthServer.Infrastructure
COPY ./CdrAuthServer.mTLS.Gateway/. /app/CdrAuthServer.mTLS.Gateway
COPY ./CdrAuthServer.TLS.Gateway/. /app/CdrAuthServer.TLS.Gateway
COPY ./CdrAuthServer.API.Logger/. /app/CdrAuthServer.API.Logger

WORKDIR /app/CdrAuthServer
RUN dotnet publish -c Release -o /app/publish/cdrauthserver
WORKDIR /app/CdrAuthServer.mTLS.Gateway
RUN dotnet publish -c Release -o /app/publish/gateway-mtls
WORKDIR /app/CdrAuthServer.TLS.Gateway
RUN dotnet publish -c Release -o /app/publish/gateway-tls

COPY supervisord.conf /app/publish/supervisord.conf

###############################################################################
# Build final layer
###############################################################################
FROM base AS final
WORKDIR /app

# Auth API ####################################################################
COPY --from=publish /app/publish/supervisord.conf .
COPY --from=publish /app/publish/cdrauthserver ./cdrauthserver
COPY --from=publish /app/publish/gateway-mtls ./gateway-mtls
COPY --from=publish /app/publish/gateway-tls ./gateway-tls

RUN apt-get update \
    && apt-get install -y supervisor \
    && apt-get install -y sudo \
    && apt-get install -y wget \
    && rm -rf /var/lib/apt/lists/*

RUN sudo cp ./gateway-mtls/Certificates/ca.crt /usr/local/share/ca-certificates/ca.crt \
    && sudo update-ca-certificates

RUN addgroup --group appgroup --gid 2000 \
    && adduser --uid 1000 --gid 2000 "appuser" \
    && chown -R appuser:appgroup /app \
    && chown -R appuser:appgroup /usr/bin \
    && chown -R appuser:appgroup /usr/local \
    && chown -R appuser:appgroup /tmp

USER appuser:appgroup

ENV ASPNETCORE_URLS=https://+:8081;https://+:8082
ENV CdrAuthServer__HttpsPort=8001
ENV CdrAuthServer__HttpPort=8080

# Entry point #################################################################
ENTRYPOINT ["/usr/bin/supervisord", "-c", "/app/supervisord.conf", "-u", "1000"]
