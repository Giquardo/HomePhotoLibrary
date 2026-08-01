using System.Security.Cryptography;
using PhotoAlbumApi.DTOs;
using PhotoAlbumApi.Models;
using PhotoAlbumApi.Repositories;

namespace PhotoAlbumApi.Services;

public interface IShareLinkService
{
    Task<ShareLink?> CreateShareLinkAsync(int albumId, int ownerUserId, int expiresInHours);
    Task<IEnumerable<ShareLink>> GetActiveShareLinksAsync(int ownerUserId);
    Task<bool> RevokeShareLinkAsync(string token, int ownerUserId);
    Task<Album?> GetSharedAlbumAsync(string token);
    Task<PhotoFileDto?> GetSharedPhotoFileAsync(string token, int photoId);
    Task<PhotoFileDto?> GetSharedPhotoThumbnailAsync(string token, int photoId);
}

public class ShareLinkService : IShareLinkService
{
    private readonly IShareLinkRepository _shareLinkRepository;
    private readonly IAlbumRepository _albumRepository;
    private readonly IImageService _imageService;

    public ShareLinkService(IShareLinkRepository shareLinkRepository, IAlbumRepository albumRepository, IImageService imageService)
    {
        _shareLinkRepository = shareLinkRepository;
        _albumRepository = albumRepository;
        _imageService = imageService;
    }

    public async Task<ShareLink?> CreateShareLinkAsync(int albumId, int ownerUserId, int expiresInHours)
    {
        // Only the album's owner can create a share link for it - reuses the
        // existing owner-scoped lookup, same as every other authenticated
        // album operation.
        var album = await _albumRepository.GetAlbumByIdAsync(albumId, ownerUserId);
        if (album == null)
        {
            return null;
        }

        var shareLink = new ShareLink
        {
            Token = GenerateToken(),
            AlbumId = albumId,
            CreatedByUserId = ownerUserId,
            ExpiresAtUtc = DateTime.UtcNow.AddHours(expiresInHours)
        };

        return await _shareLinkRepository.CreateAsync(shareLink);
    }

    public async Task<IEnumerable<ShareLink>> GetActiveShareLinksAsync(int ownerUserId)
    {
        return await _shareLinkRepository.GetActiveByOwnerAsync(ownerUserId);
    }

    public async Task<bool> RevokeShareLinkAsync(string token, int ownerUserId)
    {
        var shareLink = await _shareLinkRepository.GetByTokenForOwnerAsync(token, ownerUserId);
        if (shareLink == null || shareLink.RevokedAtUtc != null)
        {
            return false;
        }

        shareLink.RevokedAtUtc = DateTime.UtcNow;
        await _shareLinkRepository.SaveChangesAsync();
        return true;
    }

    public async Task<Album?> GetSharedAlbumAsync(string token)
    {
        var shareLink = await GetValidShareLinkAsync(token);
        if (shareLink == null)
        {
            return null;
        }

        return await _albumRepository.GetAlbumByIdIgnoringOwnerAsync(shareLink.AlbumId);
    }

    public async Task<PhotoFileDto?> GetSharedPhotoFileAsync(string token, int photoId)
    {
        var shareLink = await GetValidShareLinkAsync(token);
        if (shareLink == null)
        {
            return null;
        }

        var album = await _albumRepository.GetAlbumByIdIgnoringOwnerAsync(shareLink.AlbumId);
        var photo = album?.Photos.FirstOrDefault(p => p.Id == photoId && !p.IsDeleted);
        if (photo == null)
        {
            return null;
        }

        var fileBytes = await File.ReadAllBytesAsync(photo.FilePath);
        return new PhotoFileDto
        {
            Photo = photo,
            FileData = fileBytes,
            FileName = $"{photo.Title.Replace(" ", "_")}{photo.Extension}",
            ContentType = GetContentType(photo.Extension)
        };
    }

    public async Task<PhotoFileDto?> GetSharedPhotoThumbnailAsync(string token, int photoId)
    {
        var shareLink = await GetValidShareLinkAsync(token);
        if (shareLink == null)
        {
            return null;
        }

        var album = await _albumRepository.GetAlbumByIdIgnoringOwnerAsync(shareLink.AlbumId);
        var photo = album?.Photos.FirstOrDefault(p => p.Id == photoId && !p.IsDeleted);
        if (photo == null)
        {
            return null;
        }

        var thumbnailPath = await _imageService.GetOrCreateThumbnailAsync(photo.FilePath);
        var isRealThumbnail = thumbnailPath != photo.FilePath;
        var fileBytes = await File.ReadAllBytesAsync(thumbnailPath);

        return new PhotoFileDto
        {
            Photo = photo,
            FileData = fileBytes,
            FileName = isRealThumbnail
                ? $"{photo.Title.Replace(" ", "_")}_thumb.jpg"
                : $"{photo.Title.Replace(" ", "_")}{photo.Extension}",
            ContentType = isRealThumbnail ? "image/jpeg" : GetContentType(photo.Extension)
        };
    }

    // Expired/revoked/never-existed all collapse to the same "invalid" result -
    // the token's entropy is what protects this, not hiding which failure mode
    // occurred (unlike login, there's nothing here worth enumerating).
    private async Task<ShareLink?> GetValidShareLinkAsync(string token)
    {
        var shareLink = await _shareLinkRepository.GetByTokenAsync(token);
        if (shareLink == null || shareLink.RevokedAtUtc != null || shareLink.ExpiresAtUtc <= DateTime.UtcNow)
        {
            return null;
        }
        return shareLink;
    }

    private static string GenerateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    private static string GetContentType(string extension)
    {
        return extension switch
        {
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".bmp" => "image/bmp",
            _ => "application/octet-stream"
        };
    }
}
