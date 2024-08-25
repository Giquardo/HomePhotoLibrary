using Microsoft.AspNetCore.Mvc;
using PhotoAlbumApi.Models;
using PhotoAlbumApi.Services;
using AutoMapper;
using System.Collections.Generic;
using System.Threading.Tasks;
using PhotoAlbumApi.DTOs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;

namespace PhotoAlbumApi.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/albums")]
    [ApiVersion("1.0")]
    [ApiVersion("2.0")]
    public class AlbumsController : ControllerBase
    {
        private readonly IPhotoAlbumService _service;
        private readonly IMapper _mapper;
        private readonly LoggingService _loggingService;
        private readonly IMemoryCache _cache;

        public AlbumsController(IPhotoAlbumService service, IMapper mapper, LoggingService loggingService, IMemoryCache cache)
        {
            _service = service;
            _mapper = mapper;
            _loggingService = loggingService;
            _cache = cache;
        }

        [HttpGet]
        [MapToApiVersion("1.0")]
        public async Task<IActionResult> GetAlbumsV1()
        {
            _loggingService.LogInformation("Version: 1.0 - Fetching all albums");

            var cacheKey = "GetAlbumsV1";
            if (!_cache.TryGetValue(cacheKey, out IEnumerable<AlbumSummaryDto> albumSummaryDtos))
            {
                var albums = await _service.GetAlbumsAsync();
                albumSummaryDtos = _mapper.Map<IEnumerable<AlbumSummaryDto>>(albums);

                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromMinutes(5));

                _cache.Set(cacheKey, albumSummaryDtos, cacheEntryOptions);
            }

            _loggingService.LogInformation("Version: 1.0 - Successfully fetched all albums");

            return Ok(albumSummaryDtos);
        }

        [HttpGet]
        [MapToApiVersion("2.0")]
        public async Task<IActionResult> GetAlbumsV2()
        {
            _loggingService.LogInformation("Version: 2.0 - Fetching all albums");

            var cacheKey = "GetAlbumsV2";
            if (!_cache.TryGetValue(cacheKey, out IEnumerable<AlbumDto> albumDetailDtos))
            {
                var albums = await _service.GetAlbumsAsync();
                albumDetailDtos = _mapper.Map<IEnumerable<AlbumDto>>(albums);

                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromMinutes(5));

                _cache.Set(cacheKey, albumDetailDtos, cacheEntryOptions);
            }

            _loggingService.LogInformation("Version: 2.0 - Successfully fetched all albums");

            return Ok(albumDetailDtos);
        }

        [HttpGet("{id}")]
        [MapToApiVersion("1.0")]
        public async Task<IActionResult> GetAlbumV1(int id)
        {
            _loggingService.LogInformation($"Version: 1.0 - Fetching album with ID: {id}");

            var cacheKey = $"GetAlbumV1_{id}";
            if (!_cache.TryGetValue(cacheKey, out AlbumSummaryDto albumSummaryDto))
            {
                var album = await _service.GetAlbumAsync(id);
                if (album is not null)
                {
                    albumSummaryDto = _mapper.Map<AlbumSummaryDto>(album);

                    var cacheEntryOptions = new MemoryCacheEntryOptions()
                        .SetSlidingExpiration(TimeSpan.FromMinutes(5));

                    _cache.Set(cacheKey, albumSummaryDto, cacheEntryOptions);

                    _loggingService.LogInformation($"Version: 1.0 - Successfully fetched album with ID: {id}");
                    return Ok(albumSummaryDto);
                }
                else
                {
                    _loggingService.LogWarning($"Version: 1.0 - Album with ID: {id} not found");
                    return NotFound(new { message = "Album not found", albumId = id });
                }
            }

            return Ok(albumSummaryDto);
        }

        [HttpGet("{id}")]
        [MapToApiVersion("2.0")]
        public async Task<IActionResult> GetAlbumV2(int id)
        {
            _loggingService.LogInformation($"Version: 2.0 - Fetching album with ID: {id}");

            var cacheKey = $"GetAlbumV2_{id}";
            if (!_cache.TryGetValue(cacheKey, out AlbumDto albumDto))
            {
                var album = await _service.GetAlbumAsync(id);
                if (album is not null)
                {
                    albumDto = _mapper.Map<AlbumDto>(album);

                    var cacheEntryOptions = new MemoryCacheEntryOptions()
                        .SetSlidingExpiration(TimeSpan.FromMinutes(5));

                    _cache.Set(cacheKey, albumDto, cacheEntryOptions);

                    _loggingService.LogInformation($"Version: 2.0 - Successfully fetched album with ID: {id}");
                    return Ok(albumDto);
                }
                else
                {
                    _loggingService.LogWarning($"Version: 2.0 - Album with ID: {id} not found");
                    return NotFound(new { message = "Album not found", albumId = id });
                }
            }

            return Ok(albumDto);
        }

        [HttpPost]
        public async Task<IActionResult> AddAlbum(AlbumDto albumDto)
        {
            _loggingService.LogInformation("Adding a new album");

            var album = _mapper.Map<Album>(albumDto);
            var newAlbum = await _service.AddAlbumAsync(album);
            var newAlbumDto = _mapper.Map<AlbumDto>(newAlbum);

            _loggingService.LogInformation($"Successfully added a new album with ID: {newAlbumDto.Id}");

            return CreatedAtAction(nameof(GetAlbumV2), new { id = newAlbumDto.Id }, newAlbumDto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAlbum(int id, AlbumDto albumDto)
        {
            _loggingService.LogInformation($"Updating album with ID: {id}");

            if (albumDto == null)
            {
                _loggingService.LogWarning("Invalid album data provided");
                return BadRequest(new { message = "Invalid album data" });
            }

            var existingAlbum = await _service.GetAlbumAsync(id);
            if (existingAlbum == null)
            {
                _loggingService.LogWarning($"Album with ID: {id} not found");
                return NotFound(new { message = "Album not found", albumId = id });
            }

            // Update the existing album with the new values
            existingAlbum.Title = albumDto.Title;
            existingAlbum.Description = albumDto.Description;

            var updatedAlbum = await _service.UpdateAlbumAsync(existingAlbum);

            var updatedAlbumDto = _mapper.Map<AlbumDto>(updatedAlbum);

            _loggingService.LogInformation($"Successfully updated album with ID: {id}");

            // Update the cache
            var cacheKeyV1 = $"GetAlbumV1_{id}";
            var cacheKeyV2 = $"GetAlbumV2_{id}";
            _cache.Remove(cacheKeyV1);
            _cache.Remove(cacheKeyV2);

            return Ok(updatedAlbumDto);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAlbum(int id)
        {
            _loggingService.LogInformation($"Attempting to delete album with ID: {id}");

            var album = await _service.GetAlbumAsync(id);
            if (album is not null)
            {
                await _service.DeleteAlbumAsync(id);
                _loggingService.LogInformation($"Successfully deleted album with ID: {id}");

                // Remove from cache
                var cacheKeyV1 = $"GetAlbumV1_{id}";
                var cacheKeyV2 = $"GetAlbumV2_{id}";
                _cache.Remove(cacheKeyV1);
                _cache.Remove(cacheKeyV2);

                return NoContent();
            }
            else
            {
                _loggingService.LogWarning($"Album with ID: {id} not found");
                return NotFound(new { message = "Album not found", albumId = id });
            }
        }
    }
}