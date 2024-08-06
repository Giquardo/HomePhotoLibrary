using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using PhotoAlbumApi.Models;
using PhotoAlbumApi.Data;

namespace PhotoAlbumApi.Repositories;

public interface IPhotoRepository
{
    Task<IEnumerable<Photo>> GetPhotosAsync();
    Task<Photo?> GetPhotoByIdAsync(int id);
    Task<Photo> AddPhotoAsync(Photo photo);
    Task<Photo?> UpdatePhotoAsync(Photo photo);
    Task DeletePhotoAsync(int id);
}

public class PhotoRepository : IPhotoRepository
{
    private readonly PhotoAlbumContext _context;

    public PhotoRepository(PhotoAlbumContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Photo>> GetPhotosAsync()
    {
        return await _context.Photos.ToListAsync();
    }

    public async Task<Photo?> GetPhotoByIdAsync(int id)
    {
        return await _context.Photos.FindAsync(id);
    }

    public async Task<Photo> AddPhotoAsync(Photo photo)
    {
        await _context.Photos.AddAsync(photo);
        await _context.SaveChangesAsync();
        return photo;
    }

    public async Task<Photo?> UpdatePhotoAsync(Photo photo)
    {
        var existingPhoto = await _context.Photos.FindAsync(photo.Id);
        if (existingPhoto == null)
        {
            return null;
        }

        existingPhoto.Title = photo.Title;
        existingPhoto.Description = photo.Description;
        existingPhoto.AlbumId = photo.AlbumId;
        existingPhoto.FilePath = photo.FilePath;
        existingPhoto.Extension = photo.Extension;
        existingPhoto.DateUploaded = DateTime.Now; // Update the upload date to the current date and time

        await _context.SaveChangesAsync();
        return existingPhoto;
    }

    public async Task DeletePhotoAsync(int id)
    {
        var photo = await _context.Photos.FindAsync(id);
        if (photo != null)
        {
            _context.Photos.Remove(photo);
            await _context.SaveChangesAsync();
        }
    }
}