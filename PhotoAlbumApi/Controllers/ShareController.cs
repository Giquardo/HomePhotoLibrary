using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using PhotoAlbumApi.Services;
using PhotoAlbumApi.DTOs;

namespace PhotoAlbumApi.Controllers;

// Deliberately NOT [Authorize] at the class level, unlike AlbumController/
// PhotoController - this controller intentionally mixes authenticated
// (create/list/revoke, owner-only) and public (view/download a shared
// album, gated only by the token) actions. Keeping it separate from the
// existing controllers avoids carving [AllowAnonymous] holes into their
// GetUserId()-scoped, ownership-baked-into-every-query logic.
[ApiController]
[Route("api/shares")]
public class ShareController : ControllerBase
{
    private readonly IShareLinkService _service;
    private readonly ILoggingService _loggingService;

    public ShareController(IShareLinkService service, ILoggingService loggingService)
    {
        _service = service;
        _loggingService = loggingService;
    }

    private int GetUserId()
    {
        var userIdString = User.FindFirstValue("UserId");
        if (string.IsNullOrEmpty(userIdString))
        {
            _loggingService.LogError("User ID claim is missing.");
            throw new UnauthorizedAccessException("Invalid user ID. User is not signed in or using an invalid ID.");
        }

        if (int.TryParse(userIdString, out int userId))
        {
            return userId;
        }

        _loggingService.LogError($"Failed to parse user ID: {userIdString}");
        throw new UnauthorizedAccessException("Invalid user ID. User is not signed in or using an invalid ID.");
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateShareLink([FromBody] CreateShareLinkDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest("Invalid share link data");
        }

        try
        {
            var userId = GetUserId();
            var shareLink = await _service.CreateShareLinkAsync(dto.AlbumId, userId, dto.ExpiresInHours);
            if (shareLink == null)
            {
                return NotFound(new { message = "Album not found" });
            }

            _loggingService.LogInformation($"Created share link for album {dto.AlbumId} by user {userId}");
            var result = new ShareLinkDto
            {
                Token = shareLink.Token,
                AlbumId = shareLink.AlbumId,
                CreatedAtUtc = shareLink.CreatedAtUtc,
                ExpiresAtUtc = shareLink.ExpiresAtUtc
            };
            return StatusCode(StatusCodes.Status201Created, result);
        }
        catch (UnauthorizedAccessException ex)
        {
            _loggingService.LogError(ex.Message);
            return Unauthorized(ex.Message);
        }
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetMyShareLinks()
    {
        try
        {
            var userId = GetUserId();
            var shareLinks = await _service.GetActiveShareLinksAsync(userId);
            var result = shareLinks.Select(s => new ShareLinkDto
            {
                Token = s.Token,
                AlbumId = s.AlbumId,
                CreatedAtUtc = s.CreatedAtUtc,
                ExpiresAtUtc = s.ExpiresAtUtc
            });
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            _loggingService.LogError(ex.Message);
            return Unauthorized(ex.Message);
        }
    }

    [HttpDelete("{token}")]
    [Authorize]
    public async Task<IActionResult> RevokeShareLink(string token)
    {
        try
        {
            var userId = GetUserId();
            var revoked = await _service.RevokeShareLinkAsync(token, userId);
            if (!revoked)
            {
                return NotFound(new { message = "Share link not found" });
            }
            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            _loggingService.LogError(ex.Message);
            return Unauthorized(ex.Message);
        }
    }

    [HttpGet("{token}")]
    [EnableRateLimiting("share")]
    public async Task<IActionResult> GetSharedAlbum(string token)
    {
        var album = await _service.GetSharedAlbumAsync(token);
        if (album == null)
        {
            return NotFound(new { message = "This share link is invalid, expired, or has been revoked." });
        }

        var result = new SharedAlbumDto
        {
            Title = album.Title,
            Description = album.Description,
            Photos = album.Photos.Select(p => new SharedPhotoDto
            {
                Id = p.Id,
                Title = p.Title,
                Description = p.Description,
                Extension = p.Extension
            }).ToList()
        };
        return Ok(result);
    }

    [HttpGet("{token}/photos/{photoId}")]
    [EnableRateLimiting("share")]
    public async Task<IActionResult> GetSharedPhoto(string token, int photoId)
    {
        var photoFile = await _service.GetSharedPhotoFileAsync(token, photoId);
        if (photoFile == null)
        {
            return NotFound(new { message = "Photo not found or share link is invalid." });
        }

        return File(photoFile.FileData, photoFile.ContentType, photoFile.FileName);
    }
}
