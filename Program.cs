using Microsoft.EntityFrameworkCore;
using PhotoAlbumApi.Data;
using PhotoAlbumApi.Models;
using PhotoAlbumApi.Repositories;
using PhotoAlbumApi.Services;
using PhotoAlbumApi.Profiles;
using System.Text.Json.Serialization;
using FluentValidation.AspNetCore;
using AutoMapper;
using Serilog;


var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("Logs/serilog.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

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

// Register FluentValidation
builder.Services.AddControllers()
    .AddFluentValidation(fv => fv.RegisterValidatorsFromAssemblyContaining<Program>());

// Configure AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile).Assembly);

// Register your custom services
builder.Services.AddScoped<IPhotoAlbumService, PhotoAlbumService>(); // Replace with your actual implementation

// Create an instance of the custom logging service
var customLoggingService = new LoggingService("Logs/custom_log.txt");

// Register the custom logging service as a singleton
builder.Services.AddSingleton(customLoggingService);

var app = builder.Build();

app.MapGet("/", () => "Backend Project - Photo Album API");

// Map the controllers
app.MapControllers();

// Map the endpoints defined in AlbumsApi
app.Run("http://localhost:3000");