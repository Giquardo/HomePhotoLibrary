using Microsoft.AspNetCore.Mvc;
using PhotoAlbumApi.Models;
using PhotoAlbumApi.Services;
using AutoMapper;
using System.Collections.Generic;
using System.Threading.Tasks;
using PhotoAlbumApi.DTOs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Mysqlx;
namespace PhotoAlbumApi.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/albums")]
    [ApiVersion("1.0")]
    [ApiVersion("2.0")]
    [Authorize]
    public class AlbumController : ControllerBase
    {
        private readonly IPhotoAlbumService _service;
        private readonly IMapper _mapper;
        private readonly ILoggingService _loggingService;
        private readonly IMemoryCache _cache;

        public AlbumController(IPhotoAlbumService service, IMapper mapper, ILoggingService loggingService, IMemoryCache cache)
        {
            _service = service;
            _mapper = mapper;
            _loggingService = loggingService;
            _cache = cache;
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

        [HttpGet]
        [MapToApiVersion("1.0")]
        public async Task<IActionResult> GetAlbumsV1()
        {
            try
            {
                var userId = GetUserId();
                _loggingService.LogInformation($"Version: 1.0 - Fetching all albums for user {userId}");

                var cacheKey = $"GetAlbumsV1_{userId}";
                if (!_cache.TryGetValue(cacheKey, out IEnumerable<AlbumSummaryDto> albumSummaryDtos))
                {
                    var albums = await _service.GetAlbumsAsync(userId);
                    albumSummaryDtos = _mapper.Map<IEnumerable<AlbumSummaryDto>>(albums);

                    var cacheEntryOptions = new MemoryCacheEntryOptions()
                        .SetSlidingExpiration(TimeSpan.FromMinutes(5));

                    _cache.Set(cacheKey, albumSummaryDtos, cacheEntryOptions);
                }

                _loggingService.LogInformation($"Version: 1.0 - Successfully fetched all albums for user {userId}");

                return StatusCode(StatusCodes.Status200OK, albumSummaryDtos);
            }
            catch (UnauthorizedAccessException ex)
            {
                _loggingService.LogError(ex.Message);
                return StatusCode(StatusCodes.Status400BadRequest, ex.Message);
            }
        }

        [HttpGet]
        [MapToApiVersion("2.0")]
        public async Task<IActionResult> GetAlbumsV2()
        {
            try
            {
                var userId = GetUserId();
                _loggingService.LogInformation($"Version: 2.0 - Fetching all albums for user {userId}");

                var cacheKey = $"GetAlbumsV2_{userId}";
                if (!_cache.TryGetValue(cacheKey, out IEnumerable<AlbumSummaryDto> albumDetailDtos))
                {
                    var albums = await _service.GetAlbumsAsync(userId);
                    albumDetailDtos = _mapper.Map<IEnumerable<AlbumSummaryDto>>(albums);

                    var cacheEntryOptions = new MemoryCacheEntryOptions()
                        .SetSlidingExpiration(TimeSpan.FromMinutes(5));

                    _cache.Set(cacheKey, albumDetailDtos, cacheEntryOptions);
                }

                _loggingService.LogInformation($"Version: 2.0 - Successfully fetched all albums for user {userId}");

                return StatusCode(StatusCodes.Status200OK, albumDetailDtos);
            }
            catch (UnauthorizedAccessException ex)
            {
                _loggingService.LogError(ex.Message);
                return StatusCode(StatusCodes.Status400BadRequest, ex.Message);
            }
        }

        [HttpGet("{id}")]
        [MapToApiVersion("1.0")]
        public async Task<IActionResult> GetAlbumV1(int id)
        {
            try
            {
                var userId = GetUserId();
                _loggingService.LogInformation($"Version: 1.0 - Fetching album with ID: {id} for user {userId}");

                var cacheKey = $"GetAlbumV1_{userId}_{id}";
                if (!_cache.TryGetValue(cacheKey, out AlbumSummaryDto albumSummaryDto))
                {
                    var album = await _service.GetAlbumAsync(id, userId);
                    if (album is not null)
                    {
                        albumSummaryDto = _mapper.Map<AlbumSummaryDto>(album);

                        var cacheEntryOptions = new MemoryCacheEntryOptions()
                            .SetSlidingExpiration(TimeSpan.FromMinutes(5));

                        _cache.Set(cacheKey, albumSummaryDto, cacheEntryOptions);

                        _loggingService.LogInformation($"Version: 1.0 - Successfully fetched album with ID: {id} for user {userId}");
                        return StatusCode(StatusCodes.Status200OK, albumSummaryDto);
                    }
                    else
                    {
                        _loggingService.LogWarning($"Version: 1.0 - Album with ID: {id} not found for user {userId}");
                        return StatusCode(StatusCodes.Status404NotFound, new { message = "Album not found", albumId = id });
                    }
                }

                return StatusCode(StatusCodes.Status200OK, albumSummaryDto);
            }
            catch (UnauthorizedAccessException ex)
            {
                _loggingService.LogError(ex.Message);
                return StatusCode(StatusCodes.Status400BadRequest, ex.Message);
            }
        }

        [HttpGet("{id}")]
        [MapToApiVersion("2.0")]
        public async Task<IActionResult> GetAlbumV2(int id)
        {
            try
            {
                var userId = GetUserId();
                _loggingService.LogInformation($"Version: 2.0 - Fetching album with ID: {id} for user {userId}");

                var cacheKey = $"GetAlbumV2_{userId}_{id}";
                if (!_cache.TryGetValue(cacheKey, out AlbumSummaryDto albumDto))
                {
                    var album = await _service.GetAlbumAsync(id, userId);
                    if (album is not null)
                    {
                        albumDto = _mapper.Map<AlbumSummaryDto>(album);

                        var cacheEntryOptions = new MemoryCacheEntryOptions()
                            .SetSlidingExpiration(TimeSpan.FromMinutes(5));

                        _cache.Set(cacheKey, albumDto, cacheEntryOptions);

                        _loggingService.LogInformation($"Version: 2.0 - Successfully fetched album with ID: {id} for user {userId}");
                        return StatusCode(StatusCodes.Status200OK, albumDto);
                    }
                    else
                    {
                        _loggingService.LogWarning($"Version: 2.0 - Album with ID: {id} not found for user {userId}");
                        return StatusCode(StatusCodes.Status404NotFound, new { message = "Album not found", albumId = id });
                    }
                }

                return StatusCode(StatusCodes.Status200OK, albumDto);
            }
            catch (UnauthorizedAccessException ex)
            {
                _loggingService.LogError(ex.Message);
                return StatusCode(StatusCodes.Status400BadRequest, ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddAlbum(AlbumDto albumDto)
        {

            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest("Invalid album data");
                }
                var userId = GetUserId();
                _loggingService.LogInformation($"Adding a new album for user {userId}");

                var album = _mapper.Map<Album>(albumDto);
                album.UserId = userId;

                var newAlbum = await _service.AddAlbumAsync(album);
                var newAlbumSummaryDto = _mapper.Map<AlbumSummaryDto>(newAlbum);

                _loggingService.LogInformation($"Successfully added a new album with ID: {newAlbum.Id} for user {userId}");

                // Invalidate the cache for GetAlbumsV1 and GetAlbumsV2
                var cacheKeyV1 = $"GetAlbumsV1_{userId}";
                var cacheKeyV2 = $"GetAlbumsV2_{userId}";
                _cache.Remove(cacheKeyV1);
                _cache.Remove(cacheKeyV2);

                return StatusCode(StatusCodes.Status201Created, newAlbumSummaryDto);
            }
            catch (UnauthorizedAccessException ex)
            {
                _loggingService.LogError(ex.Message);
                return StatusCode(StatusCodes.Status401Unauthorized, ex.Message);
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAlbum(int id, AlbumDto albumDto)
        {
            try
            {
                var userId = GetUserId();
                _loggingService.LogInformation($"Updating album with ID: {id} for user {userId}");

                if (albumDto == null)
                {
                    _loggingService.LogWarning("Invalid album data provided");
                    return BadRequest("Invalid album data");
                }

                var existingAlbum = await _service.GetAlbumAsync(id, userId);
                if (existingAlbum == null)
                {
                    _loggingService.LogWarning($"Album with ID: {id} not found for user {userId}");
                    return NotFound(new { message = "Album not found", albumId = id });
                }

                existingAlbum.Title = albumDto.Title;
                existingAlbum.Description = albumDto.Description;

                var updatedAlbum = await _service.UpdateAlbumAsync(existingAlbum);

                if (updatedAlbum == null)
                {
                    _loggingService.LogError("Failed to update the album, updatedAlbum is null");
                    return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Failed to update the album" });
                }

                var albumSummaryDto = _mapper.Map<AlbumSummaryDto>(updatedAlbum);

                _loggingService.LogInformation($"Successfully updated album with ID: {updatedAlbum.Id} for user {userId}");

                var cacheKeyV1 = $"GetAlbumV1_{userId}_{id}";
                var cacheKeyV2 = $"GetAlbumV2_{userId}_{id}";
                var cacheKeyV1All = $"GetAlbumsV1_{userId}";
                _cache.Remove(cacheKeyV1);
                _cache.Remove(cacheKeyV2);
                _cache.Remove(cacheKeyV1All);

                return Ok(albumSummaryDto);
            }
            catch (UnauthorizedAccessException ex)
            {
                _loggingService.LogError(ex.Message);
                return Unauthorized(ex.Message);
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAlbum(int id)
        {
            try
            {
                var userId = GetUserId();
                _loggingService.LogInformation($"Attempting to delete album with ID: {id} for user {userId}");

                var album = await _service.GetAlbumAsync(id, userId);
                if (album is not null)
                {
                    await _service.DeleteAlbumAsync(id, userId); // Use the delete method from the service

                    _loggingService.LogInformation($"Successfully soft deleted album with ID: {id} for user {userId}");

                    // Remove from cache
                    var cacheKeyV1 = $"GetAlbumV1_{userId}_{id}";
                    var cacheKeyV2 = $"GetAlbumV2_{userId}_{id}";
                    var cacheKeyV1All = $"GetAlbumsV1_{userId}";
                    _cache.Remove(cacheKeyV1);
                    _cache.Remove(cacheKeyV2);
                    _cache.Remove(cacheKeyV1All);

                    return Ok(new { message = "Album successfully deleted", albumId = id });
                }
                else
                {
                    _loggingService.LogWarning($"Album with ID: {id} not found for user {userId}");
                    return NotFound(new { message = "Album not found", albumId = id });
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                _loggingService.LogError(ex.Message);
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("undo-delete/{id}")]
        public async Task<IActionResult> UndoDeleteAlbum(int id)
        {
            try
            {
                var userId = GetUserId();
                _loggingService.LogInformation($"Attempting to undo delete for album with ID: {id} for user {userId}");

                var album = await _service.UndoDeleteAlbumAsync(id, userId);
                if (album != null)
                {
                    _loggingService.LogInformation($"Successfully restored album with ID: {id} for user {userId}");

                    // Update the cache
                    var cacheKeyV1 = $"GetAlbumV1_{userId}_{id}";
                    var cacheKeyV2 = $"GetAlbumV2_{userId}_{id}";
                    var cacheKeyV1All = $"GetAlbumsV1_{userId}";
                    _cache.Remove(cacheKeyV1);
                    _cache.Remove(cacheKeyV2);
                    _cache.Remove(cacheKeyV1All);

                    var albumSummaryDto = _mapper.Map<AlbumSummaryDto>(album);
                    return Ok(albumSummaryDto);
                }
                else
                {
                    _loggingService.LogWarning($"Album with ID: {id} not found or not deleted for user {userId}");
                    return NotFound(new { message = "Album not found or not deleted", albumId = id });
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                _loggingService.LogError(ex.Message);
                return BadRequest(ex.Message);
            }
        }
    }
}