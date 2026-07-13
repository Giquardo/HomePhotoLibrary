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
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using PhotoAlbumApi.Models;
using PhotoAlbumApi.HealthChecks;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Configure Serilog
        var logsBasePath = builder.Configuration["Logging:BasePath"] ?? "Logs";
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File(Path.Combine(logsBasePath, "serilog.txt"), rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30)
            .CreateLogger();

        builder.Host.UseSerilog();

        // Add DbContext
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        }
        builder.Services.AddDbContext<PhotoAlbumContext>(options => options.UseMySQL(connectionString));
        builder.Services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database");

        // JWT signing key must come from a runtime secret and be strong enough for HS256.
        var jwtKey = builder.Configuration["Jwt:Key"];
        if (string.IsNullOrEmpty(jwtKey) || Encoding.UTF8.GetByteCount(jwtKey) < 32)
        {
            throw new InvalidOperationException("Jwt:Key must be set via configuration/environment variable and be at least 32 bytes (256 bits) long.");
        }

        // Register repositories
        builder.Services.AddTransient<IAlbumRepository, AlbumRepository>();
        builder.Services.AddTransient<IPhotoRepository, PhotoRepository>();
        builder.Services.AddTransient<IUserRepository, UserRepository>();
        builder.Services.AddTransient<IShareLinkRepository, ShareLinkRepository>();

        // Register services
        builder.Services.AddTransient<IImageService, ImageService>();
        builder.Services.AddTransient<IPhotoAlbumService, PhotoAlbumService>();
        builder.Services.AddTransient<IAuthenticationService, AuthenticationService>();
        builder.Services.AddTransient<IUserService, UserService>();
        builder.Services.AddTransient<IShareLinkService, ShareLinkService>();

        // Redirects are followed manually (with re-validation against the SSRF
        // block-list on every hop) instead of automatically by the handler.
        builder.Services.AddHttpClient(ImageService.DownloadHttpClientName, client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
        }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            AllowAutoRedirect = false
        });

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
        builder.Services.AddSingleton<ILoggingService>(new LoggingService(Path.Combine(logsBasePath, "custom_log.txt")));

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
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
            };
        });

        // Configure Authorization Policies
        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
        });

        // Rate-limit the login endpoint per client IP to slow down brute-force attempts.
        builder.Services.AddRateLimiter(options =>
        {
            options.AddPolicy("login", context => RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                }));
            // Shared across every policy registered below (OnRejected is process-wide,
            // not per-policy), so the message stays generic rather than login-specific.
            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.HttpContext.Response.WriteAsync("Too many requests. Please try again later.", cancellationToken);
            };

            // Cheap defense-in-depth on the public share endpoints. The share
            // token's entropy already makes brute-forcing infeasible; this just
            // slows down casual scraping/abuse.
            options.AddPolicy("share", context => RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 30,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                }));
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

        // Configure CORS
        var corsOrigins = (builder.Configuration["Cors:AllowedOrigins"] ?? "http://localhost:3000,http://localhost:5246")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.WithOrigins(corsOrigins)
                      .WithMethods("GET", "POST", "PUT", "DELETE")
                      .AllowAnyHeader();
            });
        });

        var app = builder.Build();

        // Apply pending EF migrations, then bootstrap a single admin account from
        // ADMIN_USERNAME/ADMIN_PASSWORD if the Users table is empty. No open
        // registration exists, so refuse to start rather than come up with zero
        // usable accounts. Migrate() is idempotent, safe to run on every startup.
        using (var scope = app.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<PhotoAlbumContext>();
            context.Database.Migrate();

            if (!context.Users.Any())
            {
                var adminUsername = builder.Configuration["ADMIN_USERNAME"];
                var adminPassword = builder.Configuration["ADMIN_PASSWORD"];
                if (string.IsNullOrWhiteSpace(adminUsername) || string.IsNullOrWhiteSpace(adminPassword))
                {
                    throw new InvalidOperationException("No users exist and ADMIN_USERNAME/ADMIN_PASSWORD are not set; cannot bootstrap an admin account.");
                }
                if (adminPassword.Length < 12)
                {
                    throw new InvalidOperationException("ADMIN_PASSWORD must be at least 12 characters long.");
                }
                context.Users.Add(new User
                {
                    Username = adminUsername,
                    Email = $"{adminUsername}@localhost", // not surfaced anywhere; ADMIN_EMAIL isn't part of the bootstrap contract
                    Password = BCrypt.Net.BCrypt.HashPassword(adminPassword),
                    IsAdmin = true
                });
                context.SaveChanges();
            }
        }

        app.UseAuthentication(); // Enable authentication
        app.UseAuthorization(); // Enable authorization

        // Enable CORS
        app.UseCors();

        app.UseRateLimiter();

        app.MapGet("/", () => Results.Redirect("/swagger"));

        app.MapHealthChecks("/health");

        // Map the controllers
        app.MapControllers();

        // Enable middleware to serve generated Swagger as a JSON endpoint
        app.UseSwagger();

        // Enable middleware to serve swagger UI
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Photo Album API v1.0");
        });

        app.Run();
    }
}