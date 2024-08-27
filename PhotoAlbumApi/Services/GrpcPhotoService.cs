using System.Threading.Tasks;
using Grpc.Core;
using PhotoAlbumApi.Repositories;
using PhotoAlbumApi.Proto;
using PhotoAlbumApi.Models;
using System.Security.Cryptography;
using Mysqlx.Crud;

namespace PhotoAlbumApi.Services;

public class PhotoServiceImpl : Proto.PhotoService.PhotoServiceBase
{
    private readonly IGrpcPhotoRepository _photoRepository;
    private readonly ILoggingService _loggingService;
    private readonly IImageService _imageService;

    public PhotoServiceImpl(IGrpcPhotoRepository photoRepository, ILoggingService loggingService, IImageService imageService)
    {
        _photoRepository = photoRepository;
        _loggingService = loggingService;
        _imageService = imageService;
    }

    public override async Task<GetPhotoResponse> GetPhoto(GetPhotoRequest request, ServerCallContext context)
    {
        var photo = await _photoRepository.GetPhotoByIdAsync(request.Id);
        _loggingService.LogInformation($"GRPC: Fetching photo with ID: {request.Id}");
        if (photo == null)
        {
            _loggingService.LogWarning($"GRPC: Photo with ID: {request.Id} not found");
            throw new RpcException(new Status(StatusCode.NotFound, "Photo not found"));
        }

        _loggingService.LogInformation($"GRPC: Photo with ID: {request.Id} found");
        return new GetPhotoResponse
        {
            Id = photo.Id,
            AlbumId = photo.AlbumId,
            Title = photo.Title,
            Description = photo.Description,
            Url = photo.Url,
            Extension = photo.Extension
        };
    }
    public override async Task<AddPhotoResponse> AddPhoto(AddPhotoRequest request, ServerCallContext context)
    {
        var photo = new Photo
        {
            AlbumId = request.AlbumId,
            Title = request.Title,
            Description = request.Description,
            Url = request.Url,
        };

        try
        {
            if (!string.IsNullOrEmpty(request.Url))
            {
                photo.FilePath = await _imageService.DownloadImageAsync(request.Url);
            }
            else
            {
                throw new ArgumentException("An image URL must be provided.");
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

            var addedPhoto = await _photoRepository.AddPhotoAsync(photo);
            _loggingService.LogInformation($"GRPC: Added photo with ID: {addedPhoto.Id}");

            return new AddPhotoResponse
            {
                Success = true,
                Message = "Photo added successfully with ID: " + addedPhoto.Id
            };
        }
        catch (Exception ex)
        {
            _loggingService.LogError($"GRPC: Error adding photo - {ex.Message}");
            return new AddPhotoResponse
            {
                Success = false,
                Message = $"Error adding photo: {ex.Message}"
            };
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

    public override async Task<UpdatePhotoResponse> UpdatePhoto(UpdatePhotoRequest request, ServerCallContext context)
    {
        var photo = await _photoRepository.GetPhotoByIdAsync(request.Id);
        if (photo == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Photo not found"));
        }
        photo.AlbumId = request.AlbumId;
        photo.Title = request.Title;
        photo.Description = request.Description;
        try
        {
            await _photoRepository.UpdatePhotoAsync(photo);
            _loggingService.LogInformation($"GRPC: Updated photo with ID: {photo.Id}");

            return new UpdatePhotoResponse
            {
                Success = true,
                Message = "Photo updated successfully with ID: " + photo.Id
            };
        }
        catch (Exception ex)
        {
            _loggingService.LogError($"GRPC: Error updating photo - {ex.Message}");
            return new UpdatePhotoResponse
            {
                Success = false,
                Message = $"Error updating photo: {ex.Message}"
            };
        }
    }

    public override async Task<SoftDeletePhotoResponse> SoftDeletePhoto(SoftDeletePhotoRequest request, ServerCallContext context)
    {
        var photo = await _photoRepository.GetPhotoByIdAsync(request.Id);
        if (photo == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Photo not found"));
        }
        try
        {
            await _photoRepository.SoftDeletePhotoAsync(photo.Id);
            _loggingService.LogInformation($"GRPC: Soft deleted photo with ID: {photo.Id}");

            return new SoftDeletePhotoResponse
            {
                Success = true,
                Message = "Photo soft deleted successfully with ID: " + photo.Id
            };
        }
        catch (Exception ex)
        {
            _loggingService.LogError($"GRPC: Error soft deleting photo - {ex.Message}");
            return new SoftDeletePhotoResponse
            {
                Success = false,
                Message = $"Error soft deleting photo: {ex.Message}"
            };
        }
    }
}