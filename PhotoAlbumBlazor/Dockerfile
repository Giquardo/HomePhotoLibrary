# Blazor WebAssembly publishes static output (wwwroot/_framework/*.wasm,*.dll) -
# it does not need the .NET runtime to serve, only to build.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["PhotoAlbumBlazor.csproj", "./"]
RUN dotnet restore "./PhotoAlbumBlazor.csproj"
COPY . .
RUN dotnet publish "PhotoAlbumBlazor.csproj" -c Release -o /app/publish

FROM nginx:alpine AS final
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
# The stock image's master process runs as root by default (only the worker
# processes drop to nginx:nginx). Listening on 8080 (unprivileged) means
# nothing needs root - hand ownership of everything nginx writes to at
# runtime (cache/log dirs, the pid file) to the nginx user and drop to it.
RUN chown -R nginx:nginx /usr/share/nginx/html /var/cache/nginx /var/log/nginx /etc/nginx/conf.d \
    && sed -i -e 's,/run/nginx.pid,/tmp/nginx.pid,' -e '/^user  *nginx;/d' /etc/nginx/nginx.conf
USER nginx
EXPOSE 8080
