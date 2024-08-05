using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using PhotoAlbumApi.Models;
using PhotoAlbumApi.Data;

namespace PhotoAlbumApi.Repositories;

public interface IAlbumRepository
{
    Task<IEnumerable<Album>> GetAlbumsAsync();
    Task<Album?> GetAlbumAsync(int id);
    Task<Album> AddAlbumAsync(Album album);
    Task<Album?> UpdateAlbumAsync(Album album);
    Task DeleteAlbumAsync(int id);
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
        return await _context.Albums.ToListAsync();
    }

    public async Task<Album?> GetAlbumAsync(int id)
    {
        return await _context.Albums.FindAsync(id);
    }

    public async Task<Album> AddAlbumAsync(Album album)
    {
        await _context.Albums.AddAsync(album);
        await _context.SaveChangesAsync();
        return album;
    }

    public async Task<Album?> UpdateAlbumAsync(Album album)
    {
        if (album != null)
        {
            _context.Albums.Update(album);
            await _context.SaveChangesAsync();
            return album;
        }
        return null;
    }

    public async Task DeleteAlbumAsync(int id)
    {
        var album = await _context.Albums.FindAsync(id);
        if (album != null)
        {
            _context.Albums.Remove(album);
            await _context.SaveChangesAsync();
        }
    }
}
