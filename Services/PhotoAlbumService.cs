using PhotoAlbumApi.Models;
using PhotoAlbumApi.Repositories;
using System.Security.Cryptography;

namespace PhotoAlbumApi.Services;
public interface IPhotoAlbumService
{
    Task<IEnumerable<Album>> GetAlbumsAsync(int userId);
    Task<Album?> GetAlbumAsync(int id, int userId);
    Task<Album> AddAlbumAsync(Album album);
    Task<Album?> UpdateAlbumAsync(Album album);
    Task DeleteAlbumAsync(int id, int userId);
    Task<Album> UndoDeleteAlbumAsync(int id, int userId);

    Task<IEnumerable<Photo>> GetPhotosAsync(int userId);
    Task<Photo?> GetPhotoAsync(int id, int userId);
    Task<Photo> AddPhotoAsync(Photo photo, string imageUrl = null, IFormFile file = null);
    Task<Photo?> UpdatePhotoAsync(Photo photo, string imageUrl = null, IFormFile file = null);
    Task DeletePhotoAsync(int id, int userId);
    Task<Photo> UndoDeletePhotoAsync(int id, int userId);

}

public class PhotoAlbumService : IPhotoAlbumService
{
    private readonly IAlbumRepository _albumRepository;
    private readonly IPhotoRepository _photoRepository;
    private readonly IImageService _imageService;

    public PhotoAlbumService(IAlbumRepository albumRepository, IPhotoRepository photoRepository, IImageService imageService)
    {
        _albumRepository = albumRepository;
        _photoRepository = photoRepository;
        _imageService = imageService;
    }

    public async Task<IEnumerable<Album>> GetAlbumsAsync(int userId)
    {
        return await _albumRepository.GetAlbumsAsync(userId);
    }

    public async Task<Album?> GetAlbumAsync(int id, int userId)
    {
        return await _albumRepository.GetAlbumByIdAsync(id, userId);
    }
    public async Task<Album> AddAlbumAsync(Album album)
    {
        return await _albumRepository.AddAlbumAsync(album);
    }

    public async Task<Album?> UpdateAlbumAsync(Album album)
    {
        return await _albumRepository.UpdateAlbumAsync(album);
    }

    public async Task DeleteAlbumAsync(int id, int userId)
    {
        await _albumRepository.DeleteAlbumAsync(id, userId);
    }

    public async Task<Album> UndoDeleteAlbumAsync(int id, int userId)
    {
        return await _albumRepository.UndoDeleteAlbumAsync(id, userId);
    }

    public async Task<IEnumerable<Photo>> GetPhotosAsync(int userId)
    {
        return await _photoRepository.GetPhotosAsync(userId);
    }

    public async Task<Photo?> GetPhotoAsync(int id, int userId)
    {
        return await _photoRepository.GetPhotoByIdAsync(id, userId);
    }

    public async Task<Photo> AddPhotoAsync(Photo photo, string imageUrl = null, IFormFile file = null)
    {
        if (!string.IsNullOrEmpty(imageUrl))
        {
            photo.FilePath = await _imageService.DownloadImageAsync(imageUrl);
        }
        else if (file != null && file.Length > 0)
        {
            photo.FilePath = await _imageService.SaveUploadedFileAsync(file);
        }
        else
        {
            throw new ArgumentException("Either a URL or a file must be provided.");
        }
    
        // Calculate the hash of the image
        photo.Hash = CalculateHash(photo.FilePath);
    
        // Check if an image with the same hash already exists
        var existingPhoto = await _photoRepository.GetPhotoByHashAsync(photo.Hash);
        if (existingPhoto != null)
        {
            throw new InvalidOperationException("An image with the same hash already exists.");
        }
    
        // Proceed with adding the photo
        photo.Extension = Path.GetExtension(photo.FilePath);
        photo.DateUploaded = DateTime.Now;
    
        return await _photoRepository.AddPhotoAsync(photo);
    }

    public async Task<Photo?> UpdatePhotoAsync(Photo photo, string imageUrl = null, IFormFile file = null)
    {
        var existingPhoto = await _photoRepository.GetPhotoByIdAsync(photo.Id, photo.UserId);
        if (existingPhoto == null || existingPhoto.IsDeleted)
        {
            return null;
        }

        if (!string.IsNullOrEmpty(imageUrl) && !string.Equals(imageUrl, existingPhoto.Url, StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                photo.FilePath = await _imageService.DownloadImageAsync(imageUrl);
                photo.Hash = CalculateHash(photo.FilePath);
                photo.Extension = Path.GetExtension(photo.FilePath);

                if (!string.IsNullOrEmpty(existingPhoto.FilePath))
                {
                    DeleteImage(existingPhoto.FilePath);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error occurred while downloading or processing the new image: {ex.Message}");
            }
        }
        else if (file != null && file.Length > 0)
        {
            try
            {
                photo.FilePath = await _imageService.SaveUploadedFileAsync(file);
                photo.Hash = CalculateHash(photo.FilePath);
                photo.Extension = Path.GetExtension(photo.FilePath);

                if (!string.IsNullOrEmpty(existingPhoto.FilePath))
                {
                    DeleteImage(existingPhoto.FilePath);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error occurred while saving or processing the new uploaded file: {ex.Message}");
            }
        }
        return await _photoRepository.UpdatePhotoAsync(photo);
    }

    public async Task DeletePhotoAsync(int id, int userId)
    {
        await _photoRepository.DeletePhotoAsync(id, userId);
    }

    public async Task<Photo> UndoDeletePhotoAsync(int id, int userId)
    {
        return await _photoRepository.UndoDeletePhotoAsync(id, userId);
    }

    private void DeleteImage(string filePath)
    {
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
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