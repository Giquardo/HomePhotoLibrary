using Microsoft.EntityFrameworkCore;
using PhotoAlbumApi.Data;
using PhotoAlbumApi.Models;
using PhotoAlbumApi.Repositories;
using PhotoAlbumApi.Services;
using PhotoAlbumApi.Profiles;
using System.Text.Json.Serialization;
using FluentValidation;
using FluentValidation.AspNetCore;
using AutoMapper;
using Serilog;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.Extensions.Caching.Memory;

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

// Register repositories
builder.Services.AddTransient<IAlbumRepository, AlbumRepository>();
builder.Services.AddTransient<IPhotoRepository, PhotoRepository>();

// Register services
builder.Services.AddTransient<IPhotoAlbumService, PhotoAlbumService>();

// Register FluentValidation
builder.Services.AddControllers();
builder.Services.AddFluentValidationAutoValidation()
                .AddFluentValidationClientsideAdapters();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// Configure AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile).Assembly);

// Register the custom logging service as a singleton
builder.Services.AddSingleton(new LoggingService("Logs/custom_log.txt"));

// Register Memory Cache service
builder.Services.AddMemoryCache();

// Configure API versioning
builder.Services.AddApiVersioning(config =>
{
    // Set the default API version to 1.0
    config.DefaultApiVersion = new ApiVersion(1, 0);

    // Assume the default version when unspecified
    config.AssumeDefaultVersionWhenUnspecified = true;

    // Report API versions in response headers
    config.ReportApiVersions = true;

    // Use URL segment to read the API version
    config.ApiVersionReader = new UrlSegmentApiVersionReader();
});

// Configure the Swagger generator
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Photo Album API", Version = "v1.0" });

    // Add this to resolve conflicting actions
    c.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());
});

var app = builder.Build();

app.MapGet("/", () => "Backend Project - Photo Album API");

// Map the controllers
app.MapControllers();


// Enable middleware to serve generated Swagger as a JSON endpoint
app.UseSwagger();

// Enable middleware to serve swagger UI
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Photo Album API v1.0");
});

// Map the endpoints defined in AlbumsApi
app.Run("http://localhost:3000");
