using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PhotoAlbumApi.Data;

// Used only by `dotnet ef` tooling. Program.cs throws at startup if the real
// connection string / JWT key aren't configured, which would otherwise block
// EF's default design-time discovery (it tries to build the app host). This
// factory bypasses Program.cs entirely; no connection is ever opened for
// `migrations add`.
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<PhotoAlbumContext>
{
    public PhotoAlbumContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PhotoAlbumContext>();
        optionsBuilder.UseMySQL("Server=localhost;Port=3306;Database=photoalbum;Uid=root;Pwd=root;");
        return new PhotoAlbumContext(optionsBuilder.Options);
    }
}
