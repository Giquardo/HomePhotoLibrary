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
    }
}