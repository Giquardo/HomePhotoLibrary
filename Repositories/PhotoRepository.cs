using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using PhotoAlbumApi.Models;
using PhotoAlbumApi.Data;
using System.Net.Http;


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
        if (string.IsNullOrEmpty(photo.FilePath) && !string.IsNullOrEmpty(photo.Url))
        {
            photo.FilePath = await DownloadImageAsync(photo.Url);
        }

        photo.Hash = CalculateHash(photo.FilePath);
        photo.Extension = Path.GetExtension(photo.FilePath);

        // Check if a photo with the same hash already exists
        var existingPhoto = await _context.Photos.FirstOrDefaultAsync(p => p.Hash == photo.Hash);
        if (existingPhoto != null)
        {
            // Handle the case where the photo already exists
            // For example, you could throw an exception or return the existing photo
            throw new InvalidOperationException("A photo with the same hash already exists.");
        }

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

        existingPhoto.AlbumId = photo.AlbumId;
        existingPhoto.Title = photo.Title;
        existingPhoto.DateUploaded = photo.DateUploaded;
        existingPhoto.Description = photo.Description;
        existingPhoto.Extension = Path.GetExtension(photo.FilePath);
        existingPhoto.FilePath = photo.FilePath;
        existingPhoto.Url = photo.Url;
        existingPhoto.Hash = CalculateHash(photo.FilePath);

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

    private string GenerateFilePath(string fileName)
    {
        var relativePath = Path.Combine("Data", "Files");
        if (!Directory.Exists(relativePath))
        {
            Directory.CreateDirectory(relativePath);
        }
        return Path.Combine(relativePath, fileName);
    }

    private async Task<string> DownloadImageAsync(string imageUrl)
    {
        using (HttpClient client = new HttpClient())
        {
            HttpResponseMessage response = await client.GetAsync(imageUrl);
            response.EnsureSuccessStatusCode();

            byte[] imageBytes = await response.Content.ReadAsByteArrayAsync();
            var fileName = Path.GetFileName(new Uri(imageUrl).AbsolutePath);
            var filePath = GenerateFilePath(fileName);

            await File.WriteAllBytesAsync(filePath, imageBytes);
            return filePath;
        }
    }

    private string CalculateHash(string filePath)
    {
        using (var sha256 = SHA256.Create())
        {
            using (var stream = File.OpenRead(filePath))
            {
                var hashBytes = sha256.ComputeHash(stream);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
        }
    }
}