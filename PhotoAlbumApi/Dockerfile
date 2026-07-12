# Use the official ASP.NET Core runtime as a base image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://0.0.0.0:8080

# curl is used by the compose healthcheck to call /health; not present by default.
RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

# Use the official ASP.NET Core SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Backend_dev.csproj", "./"]
RUN dotnet restore "./Backend_dev.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "Backend_dev.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Backend_dev.csproj" -c Release -o /app/publish

# Use the base image to run the app
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
# Pre-create the volume mount points so Docker seeds fresh named volumes with
# app:app ownership on first use - they don't otherwise exist in this image
# (the app creates them lazily), so a mounted volume would default to root.
RUN mkdir -p /app/Logs /app/Data/Files && chown -R app:app /app
USER app
ENTRYPOINT ["dotnet", "Backend_dev.dll"]
