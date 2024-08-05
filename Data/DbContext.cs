using Microsoft.EntityFrameworkCore;
using PhotoAlbumApi.Models;

namespace PhotoAlbumApi.Data;
public class PhotoAlbumContext : DbContext
{
    public DbSet<Album> Albums { get; set; }
    public DbSet<Photo> Photos { get; set; }

    public PhotoAlbumContext(DbContextOptions<PhotoAlbumContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Photo>()
            .HasOne(s => s.Album)
            .WithMany(c => c.Photos);

        modelBuilder.Entity<Album>()
            .HasMany(s => s.Photos)
            .WithOne(c => c.Album);

        // Seed data
        modelBuilder.Entity<Album>().HasData(
            new Album
            {
                Id = 1,
                Title = "AI",
                Description = "Album for AI related photos"
            }
        );
    }
}
