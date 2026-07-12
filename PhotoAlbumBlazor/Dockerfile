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
EXPOSE 8080
