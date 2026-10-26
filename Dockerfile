# Northwind Ops: built and run on .NET 11 RC3 images, independent of what the host has installed.
FROM mcr.microsoft.com/dotnet/sdk:11.0.100-rc.3 AS build
WORKDIR /src
# Restore with the whole project present: restoring from the .csproj alone (to cache the layer) leaves out the
# package that carries _framework/blazor.web.js, and the published app then serves pages Blazor never starts on.
COPY global.json ./
COPY src/ src/
RUN dotnet publish src/NorthwindOps/NorthwindOps.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:11.0.0-rc.3 AS final
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
# The commit being built, for /version. Set last so a new commit doesn't invalidate any cached layer above.
ARG GIT_SHA=unknown
ENV GIT_SHA=$GIT_SHA
ENTRYPOINT ["dotnet", "NorthwindOps.dll"]
