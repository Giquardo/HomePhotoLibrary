using Microsoft.EntityFrameworkCore;
using PhotoAlbumApi.Models;
using PhotoAlbumApi.Data;

namespace PhotoAlbumApi.Repositories;

public interface IPhotoRepository
{
    Task<IEnumerable<Photo>> GetPhotosAsync(int userId);
    Task<Photo?> GetPhotoByIdAsync(int id, int userId);
    Task<Photo> AddPhotoAsync(Photo photo);
    Task<Photo?> UpdatePhotoAsync(Photo photo);
    Task DeletePhotoAsync(int id, int userId);
    Task<Photo> UndoDeletePhotoAsync(int id, int userId);
    Task<Photo?> GetPhotoByHashAsync(string hash);

}

public class PhotoRepository : IPhotoRepository
{
    private readonly PhotoAlbumContext _context;

    public PhotoRepository(PhotoAlbumContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Photo>> GetPhotosAsync(int userId)
    {
        return await _context.Photos.Where(p => p.UserId == userId && !p.IsDeleted).ToListAsync();
    }

    public async Task<Photo?> GetPhotoByIdAsync(int id, int userId)
    {
        return await _context.Photos.FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId && !p.IsDeleted);
    }

    public async Task<Photo> AddPhotoAsync(Photo photo)
    {
        await _context.Photos.AddAsync(photo);
        await _context.SaveChangesAsync();
        return photo;
    }

    public async Task<Photo?> UpdatePhotoAsync(Photo photo)
    {
        var existingPhoto = await _context.Photos.AsNoTracking().FirstOrDefaultAsync(p => p.Id == photo.Id);
        if (existingPhoto == null || existingPhoto.IsDeleted)
        {
            return null;
        }

        var originalUrl = existingPhoto.Url;

        if (!string.Equals(photo.Url, originalUrl, StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(photo.FilePath) || string.IsNullOrEmpty(photo.Hash) || string.IsNullOrEmpty(photo.Extension))
            {
                throw new InvalidOperationException("New image file details are missing.");
            }
        }

        _context.Attach(photo);
        _context.Entry(photo).State = EntityState.Modified; // Set state to modified

        try
        {
            await _context.SaveChangesAsync();
            return photo;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task DeletePhotoAsync(int id, int userId)
    {
        var photo = await _context.Photos.FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);
        if (photo != null && !photo.IsDeleted)
        {
            photo.IsDeleted = true;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<Photo> UndoDeletePhotoAsync(int id, int userId)
    {
        var photo = await _context.Photos.FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);
        if (photo != null && photo.IsDeleted)
        {
            photo.IsDeleted = false;
            await _context.SaveChangesAsync();
        }
        return photo;
    }

    public async Task<Photo?> GetPhotoByHashAsync(string hash)
    {
        return await _context.Photos.FirstOrDefaultAsync(p => p.Hash == hash && !p.IsDeleted);
    }
}