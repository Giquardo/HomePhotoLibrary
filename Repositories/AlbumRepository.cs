using Microsoft.EntityFrameworkCore;
using PhotoAlbumApi.Models;
using PhotoAlbumApi.Data;

namespace PhotoAlbumApi.Repositories;

public interface IAlbumRepository
{
    Task<IEnumerable<Album>> GetAlbumsAsync(int userId);
    Task<Album?> GetAlbumByIdAsync(int id, int userId);
    Task<Album> AddAlbumAsync(Album album);
    Task<Album?> UpdateAlbumAsync(Album album);
    Task DeleteAlbumAsync(int id, int userId);
    Task<Album> UndoDeleteAlbumAsync(int id, int userId);
}

public class AlbumRepository : IAlbumRepository
{
    private readonly PhotoAlbumContext _context;

    public AlbumRepository(PhotoAlbumContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Album>> GetAlbumsAsync(int userId)
    {
        return await _context.Albums
            .Include(a => a.Photos)
            .Where(a => a.UserId == userId && !a.IsDeleted)
            .ToListAsync();
    }

    public async Task<Album?> GetAlbumByIdAsync(int id, int userId)
    {
        return await _context.Albums
            .Include(a => a.Photos)
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId && !a.IsDeleted); // Filter out deleted albums
    }

    public async Task<Album> AddAlbumAsync(Album album)
    {
        await _context.Albums.AddAsync(album);
        await _context.SaveChangesAsync();
        return album;
    }

    public async Task<Album?> UpdateAlbumAsync(Album album)
    {
        var existingAlbum = await _context.Albums.FindAsync(album.Id);
        if (existingAlbum == null || existingAlbum.IsDeleted) // Check if album is deleted
        {
            return null;
        }

        existingAlbum.Title = album.Title;
        existingAlbum.Description = album.Description;

        await _context.SaveChangesAsync();
        return existingAlbum;
    }

    public async Task DeleteAlbumAsync(int id, int userId)
    {
        var album = await _context.Albums.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
        if (album != null && !album.IsDeleted)
        {
            album.IsDeleted = true;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<Album> UndoDeleteAlbumAsync(int id, int userId)
    {
        var album = await _context.Albums.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
        if (album != null && album.IsDeleted)
        {
            album.IsDeleted = false;
            await _context.SaveChangesAsync();
        }
        return album;
    }

}