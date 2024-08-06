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

// Register services
builder.Services.AddTransient<IPhotoAlbumService, PhotoAlbumService>();

// Register repositories
builder.Services.AddTransient<IAlbumRepository, AlbumRepository>();
builder.Services.AddTransient<IPhotoRepository, PhotoRepository>();

builder.Services.AddControllers();

var app = builder.Build();

// Download images for seeded photos
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<PhotoAlbumContext>();
    var imageDownloadService = new ImageDownloadService();

    var photos = context.Photos.Where(p => !string.IsNullOrEmpty(p.Url)).ToList();
    foreach (var photo in photos)
    {
        await imageDownloadService.DownloadImageAsync(photo);
        context.Update(photo);
    }

    await context.SaveChangesAsync();
}

app.MapGet("/", () => "Backend Project - Photo Album API");

// Map the controllers
app.MapControllers();

app.Run("http://localhost:3000");