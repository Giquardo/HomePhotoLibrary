# Use the official ASP.NET Core SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["PhotoAlbumBlazor.csproj", "./"]
RUN dotnet restore "./PhotoAlbumBlazor.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "PhotoAlbumBlazor.csproj" -c Release -o /app/build
RUN dotnet publish "PhotoAlbumBlazor.csproj" -c Release -o /app/publish

# Use the official ASP.NET Core runtime as a base image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "PhotoAlbumBlazor.dll"]