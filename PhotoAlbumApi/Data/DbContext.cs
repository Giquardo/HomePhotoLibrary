using Microsoft.EntityFrameworkCore;
using PhotoAlbumApi.Models;

namespace PhotoAlbumApi.Data;
public class PhotoAlbumContext : DbContext
{
    public DbSet<Album> Albums { get; set; }
    public DbSet<Photo> Photos { get; set; }
    public DbSet<User> Users { get; set; }

    public PhotoAlbumContext(DbContextOptions<PhotoAlbumContext> options) : base(options)
    { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Define relationships with cascading deletes
        modelBuilder.Entity<Photo>()
            .HasOne(p => p.Album)
            .WithMany(a => a.Photos)
            .HasForeignKey(p => p.AlbumId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Album>()
            .HasOne(a => a.User)
            .WithMany(u => u.Albums)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Seed data for Albums
        modelBuilder.Entity<Album>().HasData(
            new Album
            {
                Id = 1,
                Title = "AI",
                Description = "Album for AI related photos",
                UserId = 1
            },
            new Album
            {
                Id = 2,
                Title = "Nature",
                Description = "Album for nature related photos",
                UserId = 1
            },
            new Album
            {
                Id = 3,
                Title = "Travel",
                Description = "Album for travel related photos",
                UserId = 1
            },
            new Album
            {
                Id = 4,
                Title = "Family",
                Description = "Album for family related photos",
                UserId = 1
            },
            new Album
            {
                Id = 5,
                Title = "Food",
                Description = "Album for food related photos",
                UserId = 1
            }
        );

        // Seed data for Users
        modelBuilder.Entity<User>().HasData(
            new User
            {
                Id = 1,
                Username = "giquardo",
                Email = "giquardo@gmail.com",
                Password = "123",
                IsAdmin = true
            },
            new User
            {
                Id = 2,
                Username = "alessia",
                Email = "alessia@gmail.com",
                Password = "123",
                IsAdmin = false
            }
        );
    }
}