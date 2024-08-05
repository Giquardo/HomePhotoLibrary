using Microsoft.EntityFrameworkCore;
using PhotoAlbumApi.Data;
using PhotoAlbumApi.Models;
using PhotoAlbumApi.Repositories;
using PhotoAlbumApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Add DbContext
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
}
builder.Services.AddDbContext<PhotoAlbumContext>(options => options.UseMySQL(connectionString));

// Reggister services
builder.Services.AddTransient<IPhotoAlbumService, PhotoAlbumService>();

// Register repositories
builder.Services.AddTransient<IAlbumRepository, AlbumRepository>();

var app = builder.Build();

app.MapGet("/", () => "Backend Project - Photo Album API");

app.MapGet("/api/albums", async (IPhotoAlbumService service) =>
{
    var albums = await service.GetAlbumsAsync();
    return Results.Ok(albums);
});

app.MapGet("/api/albums/{id}", async (int id, IPhotoAlbumService service) =>
{
    var album = await service.GetAlbumAsync(id);
    if (album is not null)
    {
        return Results.Ok(album);
    }
    else
    {
        return Results.Json(new { message = "Album not found", albumId = id }, statusCode: 404);
    }
});

app.MapPost("/api/albums", async (Album album, IPhotoAlbumService service) =>
{
    var newAlbum = await service.AddAlbumAsync(album);
    return Results.Created($"/api/albums/{newAlbum.Id}", newAlbum);
});

app.MapPut("/api/albums/{id}", async (int id, Album updatedAlbum, IPhotoAlbumService service) =>
{
    var album = await service.GetAlbumAsync(id);
    if (album is not null)
    {
        album.Title = updatedAlbum.Title;
        album.Description = updatedAlbum.Description;
        await service.UpdateAlbumAsync(album);
        return Results.Ok(album);
    }
    else
    {
        return Results.Json(new { message = "Album not found", albumId = id }, statusCode: 404);
    }
});

app.MapDelete("/api/albums/{id}", async (int id, IPhotoAlbumService service) =>
{
    await service.DeleteAlbumAsync(id);
    return Results.NoContent();
});


app.Run("http://localhost:3000");
