using Microsoft.EntityFrameworkCore;
using PhotoAlbumApi.Data;
using PhotoAlbumApi.Repositories;
using PhotoAlbumApi.Services;
using PhotoAlbumApi.Profiles;
using FluentValidation;
using FluentValidation.AspNetCore;
using Serilog;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;
using System.Text.Json.Serialization;
using PhotoAlbumApi.Swagger;
using Microsoft.AspNetCore.Server.Kestrel.Core;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Configure Kestrel to use HTTP/2 for gRPC on port 3001 and HTTP/1.1 for the rest on port 3000
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.ListenLocalhost(3000, listenOptions =>
            {
                listenOptions.Protocols = HttpProtocols.Http1;
            });
            options.ListenLocalhost(3001, listenOptions =>
            {
                listenOptions.Protocols = HttpProtocols.Http2;
            });
        });

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
        builder.Services.AddTransient<IUserRepository, UserRepository>();

        // Register services
        builder.Services.AddTransient<IImageService, ImageService>();
        builder.Services.AddTransient<IPhotoAlbumService, PhotoAlbumService>();
        builder.Services.AddTransient<IAuthenticationService, AuthenticationService>();
        builder.Services.AddTransient<IUserService, UserService>();

        // Register FluentValidation
        builder.Services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            });
        builder.Services.AddFluentValidationAutoValidation()
                        .AddFluentValidationClientsideAdapters();
        builder.Services.AddValidatorsFromAssemblyContaining<Program>();

        // Configure AutoMapper
        builder.Services.AddAutoMapper(typeof(MappingProfile).Assembly);

        // Register the custom logging service as a singleton
        builder.Services.AddSingleton<ILoggingService>(new LoggingService("Logs/custom_log.txt"));

        // Register Memory Cache service
        builder.Services.AddMemoryCache();

        // Configure API versioning
        builder.Services.AddApiVersioning(config =>
        {
            config.DefaultApiVersion = new ApiVersion(1, 0);
            config.AssumeDefaultVersionWhenUnspecified = true;
            config.ReportApiVersions = true;
            config.ApiVersionReader = new UrlSegmentApiVersionReader();
        });

        // Configure JWT Authentication
        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = builder.Configuration["Jwt:Issuer"],
                ValidAudience = builder.Configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
            };
        });

        // Configure Authorization Policies
        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
        });

        // Configure the Swagger generator
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new() { Title = "Photo Album API", Version = "v1.0" });

            // Add JWT Authentication to Swagger
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = @"JWT Authorization header using the Bearer scheme." + "\r\n\r\n" +
                              "Enter 'Bearer' [space] and then your token in the text input below." + "\r\n\r\n" +
                              ": 'Bearer 12345abcdef'" + "\r\n\r\n",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });

            c.AddSecurityRequirement(new OpenApiSecurityRequirement()
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        },
                        Scheme = "oauth2",
                        Name = "Bearer",
                        In = ParameterLocation.Header,
                    },
                    new List<string>()
                }
            });

            // Add this to resolve conflicting actions
            c.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());

            // Add the custom operation filter for file uploads
            c.OperationFilter<FileUploadOperationFilter>();
        });

        // Add gRPC services
        builder.Services.AddGrpc();
        builder.Services.AddScoped<IGrpcPhotoRepository, GrpcPhotoRepository>();

        // Configure CORS
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(builder =>
            {
                builder.AllowAnyOrigin()
                       .AllowAnyMethod()
                       .AllowAnyHeader();
            });
        });

        var app = builder.Build();

        app.UseAuthentication(); // Enable authentication
        app.UseAuthorization(); // Enable authorization

        // Enable CORS
        app.UseCors();

        app.MapGet("/", () => Results.Redirect("/swagger"));

        // Map the controllers
        app.MapControllers();

        // Map gRPC services
        app.MapGrpcService<PhotoServiceImpl>().RequireHost("localhost:3001");

        // Enable middleware to serve generated Swagger as a JSON endpoint
        app.UseSwagger();

        // Enable middleware to serve swagger UI
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Photo Album API v1.0");
        });

        app.Run("http://localhost:3000");
    }
}