using Microsoft.EntityFrameworkCore;
using PhotoAlbumApi.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PhotoAlbumApi.Data;
public class PhotoAlbumContext : DbContext
{

    public DbSet<Album> Albums { get; set; }
    public DbSet<Photo> Photos { get; set; }

    public PhotoAlbumContext(DbContextOptions<PhotoAlbumContext> options) : base(options)
    { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Photo>()
            .HasOne<Album>()
            .WithMany(a => a.Photos)
            .HasForeignKey(p => p.AlbumId);

        // Seed data for Albums
        modelBuilder.Entity<Album>().HasData(
            new Album
            {
                Id = 1,
                Title = "AI",
                Description = "Album for AI related photos"
            },
            new Album
            {
                Id = 2,
                Title = "Nature",
                Description = "Album for nature related photos"
            },
            new Album
            {
                Id = 3,
                Title = "Travel",
                Description = "Album for travel related photos"
            },
            new Album
            {
                Id = 4,
                Title = "Family",
                Description = "Album for family related photos"
            },
            new Album
            {
                Id = 5,
                Title = "Food",
                Description = "Album for food related photos"
            }
        );
    }
}
