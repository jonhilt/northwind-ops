# Northwind Ops on .NET 11 RC2, independent of what the host has installed.
#
# RC2 shipped before its container images, so for now the build stage installs the RC2 SDK on the RC1 SDK image
# and publishes self-contained (the RC2 runtime travels with the app) onto the version-neutral runtime-deps image.
# Once mcr.microsoft.com/dotnet/sdk:11.0.100-rc.2 and aspnet:11.0.0-rc.2 exist, go back to those two images and a
# framework-dependent publish.
FROM mcr.microsoft.com/dotnet/sdk:11.0.100-rc.1 AS build
ARG TARGETARCH
RUN curl -fsSL https://dot.net/v1/dotnet-install.sh | bash -s -- --version 11.0.100-rc.2.26504.105 --install-dir /usr/share/dotnet
WORKDIR /src
# Restore with the whole project present: restoring from the .csproj alone (to cache the layer) leaves out the
# package that carries _framework/blazor.web.js, and the published app then serves pages Blazor never starts on.
COPY global.json ./
COPY src/ src/
RUN dotnet publish src/NorthwindOps/NorthwindOps.csproj -c Release -o /app \
    --runtime linux-$([ "$TARGETARCH" = "arm64" ] && echo arm64 || echo x64) --self-contained

FROM mcr.microsoft.com/dotnet/runtime-deps:11.0.0-rc.1 AS final
WORKDIR /app
COPY --from=build /app .

# "Demo" isn't Production, so the EnvironmentView banner shows. The database is rebuilt in /tmp
# on start and every hour, and TLS is terminated by the platform's proxy in front of the container.
ENV ASPNETCORE_ENVIRONMENT=Demo \
    ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_FORWARDEDHEADERS_ENABLED=true \
    ConnectionStrings__Northwind="Data Source=/tmp/northwind.db"

EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["./NorthwindOps"]
