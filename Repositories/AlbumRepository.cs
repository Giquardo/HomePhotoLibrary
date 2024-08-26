using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using PhotoAlbumApi.Models;
using PhotoAlbumApi.Data;

namespace PhotoAlbumApi.Repositories;

public interface IAlbumRepository
{
    Task<IEnumerable<Album>> GetAlbumsAsync();
    Task<Album?> GetAlbumByIdAsync(int id);
    Task<Album> AddAlbumAsync(Album album);
    Task<Album?> UpdateAlbumAsync(Album album);
    Task DeleteAlbumAsync(int id);
    Task<Album> UndoDeleteAlbumAsync(int id);
}

public class AlbumRepository : IAlbumRepository
{
    private readonly PhotoAlbumContext _context;

    public AlbumRepository(PhotoAlbumContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Album>> GetAlbumsAsync()
    {
        return await _context.Albums
            .Include(a => a.Photos)
            .Where(a => !a.IsDeleted) // Filter out deleted albums
            .ToListAsync();
    }

    public async Task<Album?> GetAlbumByIdAsync(int id)
    {
        return await _context.Albums
            .Include(a => a.Photos)
            .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted); // Filter out deleted albums
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

    public async Task DeleteAlbumAsync(int id)
    {
        var album = await _context.Albums.FindAsync(id);
        if (album != null && !album.IsDeleted) // Check if album is already deleted
        {
            album.IsDeleted = true; // Mark as deleted
            await _context.SaveChangesAsync();
        }
    }

    public async Task<Album> UndoDeleteAlbumAsync(int id)
    {
        var album = await _context.Albums.FindAsync(id);
        if (album != null && album.IsDeleted) // Check if album is deleted
        {
            album.IsDeleted = false; // Mark as not deleted
            await _context.SaveChangesAsync();
        }
        return album;
    }

}