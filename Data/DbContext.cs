using Microsoft.EntityFrameworkCore;
using PhotoAlbumApi.Models;
using PhotoAlbumApi.Services;
using System.Threading.Tasks;

namespace PhotoAlbumApi.Data
{
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
                .WithMany(c => c.Photos)
                .HasForeignKey(p => p.AlbumId);

            modelBuilder.Entity<Album>()
                .HasMany(s => s.Photos)
                .WithOne(c => c.Album);

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

            // Seed data for Photos
            modelBuilder.Entity<Photo>().HasData(
                new Photo
                {
                    Id = 1,
                    Title = "AI Art Creation",
                    Url = "https://news.ubc.ca/wp-content/uploads/2023/08/AdobeStock_559145847.jpeg",
                    AlbumId = 1 // Only set the foreign key, not the navigation property
                }
            );
        }
    }
}