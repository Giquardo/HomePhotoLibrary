using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using PhotoAlbumApi.Models;
using PhotoAlbumApi.Data;
using System.Net.Http;
using Mysqlx.Crud;
using PhotoAlbumApi.Services;

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
    private readonly LoggingService _loggingService;

    public PhotoRepository(PhotoAlbumContext context, LoggingService loggingService)
    {
        _context = context;
        _loggingService = loggingService;
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
        photo.DateUploaded = DateTime.Now;

        // Check if a photo with the same hash already exists
        var existingPhoto = await _context.Photos.FirstOrDefaultAsync(p => p.Hash == photo.Hash);
        if (existingPhoto != null)
        {
            _loggingService.LogWarning("DATABASE: A photo with the same hash already exists.");
            throw new InvalidOperationException("A photo with the same hash already exists.");
        }

        await _context.Photos.AddAsync(photo);
        await _context.SaveChangesAsync();
        return photo;
    }

    public async Task<Photo?> UpdatePhotoAsync(Photo photo)
    {
        _loggingService.LogInformation($"DATABASE: Starting update for photo with ID: {photo.Id} and URL: {photo.Url}");

        // Fetch the existing photo from the database with AsNoTracking to avoid tracking issues
        var existingPhoto = await _context.Photos.AsNoTracking().FirstOrDefaultAsync(p => p.Id == photo.Id);
        if (existingPhoto == null)
        {
            _loggingService.LogWarning($"DATABASE: Photo with ID: {photo.Id} not found.");
            return null;
        }

        _loggingService.LogInformation($"DATABASE: Found existing photo with ID: {existingPhoto.Id} and URL: {existingPhoto.Url}");

        // Store the original URL for comparison
        var originalUrl = existingPhoto.Url;
        _loggingService.LogInformation($"DATABASE: Original URL: {originalUrl}, Incoming URL: {photo.Url}");

        // Check if the URL has changed
        if (!string.Equals(photo.Url, originalUrl, StringComparison.OrdinalIgnoreCase))
        {
            _loggingService.LogInformation("DATABASE: URLs are different. Downloading the new image file.");

            // Download the new image and update related fields
            try
            {
                photo.FilePath = await DownloadImageAsync(photo.Url);
                photo.Hash = CalculateHash(photo.FilePath);
                photo.Extension = Path.GetExtension(photo.FilePath);

                _loggingService.LogInformation($"DATABASE: Downloaded new image file at: {photo.FilePath} with hash: {photo.Hash}");

                // Delete the old image file
                if (!string.IsNullOrEmpty(existingPhoto.FilePath))
                {
                    DeleteImage(existingPhoto.FilePath);
                    _loggingService.LogInformation($"DATABASE: Deleted old image file at: {existingPhoto.FilePath}");
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"DATABASE: Error occurred while downloading or processing the new image: {ex.Message}");
                throw; // Optionally, handle or propagate the exception
            }
        }
        else
        {
            _loggingService.LogInformation("DATABASE: URLs are the same. No need to download a new image file.");
        }

        // Attach the updated photo to the context
        _context.Attach(photo);
        _context.Entry(photo).State = EntityState.Modified; // Set state to modified

        try
        {
            await _context.SaveChangesAsync();
            _loggingService.LogInformation($"DATABASE: Successfully updated photo with ID: {photo.Id}");
            return photo;
        }
        catch (Exception ex)
        {
            _loggingService.LogError($"DATABASE: Error occurred while saving the updated photo: {ex.Message}");
            return null;
        }
    }

    public async Task DeletePhotoAsync(int id)
    {
        var photo = await _context.Photos.FindAsync(id);
        if (photo != null)
        {
            DeleteImage(photo.FilePath);
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

    private void DeleteImage(string filePath)
    {
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
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