using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using PhotoAlbumApi.Models;
using PhotoAlbumApi.Validators;
using PhotoAlbumApi.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using AutoMapper;
using PhotoAlbumApi.Services;


namespace PhotoAlbumApi.Controllers;
[ApiController]
[Route("api/photos")]
public class PhotoController : ControllerBase
{
    private readonly IPhotoAlbumService _service;
    private readonly IMapper _mapper;
    private readonly LoggingService _loggingService;
    private readonly IMemoryCache _cache;

    public PhotoController(IPhotoAlbumService service, IMapper mapper, LoggingService loggingService, IMemoryCache cache)
    {
        _service = service;
        _mapper = mapper;
        _loggingService = loggingService;
        _cache = cache;
    }

    // GET: api/Photos
    [HttpGet]
    public async Task<ActionResult> GetPhotos()
    {
        _loggingService.LogInformation("Fetching all photos");
        var cacheKey = "GetPhotos";
        if (!_cache.TryGetValue(cacheKey, out IEnumerable<PhotoDto> photoDtos))
        {
            var photos = await _service.GetPhotosAsync();
            photoDtos = _mapper.Map<IEnumerable<PhotoDto>>(photos);

            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(5));

            _cache.Set(cacheKey, photoDtos, cacheEntryOptions);
        }
        _loggingService.LogInformation("Successfully fetched all photos");
        return Ok(photoDtos);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult> GetPhoto(int id)
    {
        _loggingService.LogInformation($"Fetching photo with ID: {id}");
        var cacheKey = $"GetPhoto_{id}";
        if (!_cache.TryGetValue(cacheKey, out PhotoDto photoDto))
        {
            var photo = await _service.GetPhotoAsync(id);
            if (photo == null)
            {
                _loggingService.LogWarning($"Photo with ID: {id} not found");
                return NotFound();
            }
            photoDto = _mapper.Map<PhotoDto>(photo);

            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(5));

            _cache.Set(cacheKey, photoDto, cacheEntryOptions);
            _loggingService.LogInformation($"Successfully fetched photo with ID: {id}");
        }
        return Ok(photoDto);
    }

    [HttpPost]
    public async Task<ActionResult> AddPhoto(PhotoDto photoDto)
    {
        _loggingService.LogInformation("Adding a new photo");
        try
        {
            var photo = _mapper.Map<Photo>(photoDto);
            var addedPhoto = await _service.AddPhotoAsync(photo);
            _loggingService.LogInformation($"Successfully added a new photo with ID: {addedPhoto.Id}");
            return CreatedAtAction(nameof(GetPhoto), new { id = addedPhoto.Id }, addedPhoto);
        }
        catch (InvalidOperationException ex)
        {
            _loggingService.LogError(ex, "Error adding photo");
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdatePhoto(int id, PhotoDto photoDto)
    {
        _loggingService.LogInformation($"Updating photo with ID: {id}");

        if (photoDto == null)
        {
            _loggingService.LogWarning("Photo data is missing");
            return BadRequest(new { message = "Photo data is missing" });
        }

        // Retrieve the existing photo from the service/repository
        var existingPhoto = await _service.GetPhotoAsync(id);
        if (existingPhoto == null)
        {
            _loggingService.LogWarning($"Photo with ID: {id} not found");
            return NotFound(new { message = "Photo not found", photoId = id });
        }

        // Assign updated properties to the existing photo object
        existingPhoto.AlbumId = photoDto.AlbumId;
        existingPhoto.Title = photoDto.Title;
        existingPhoto.Description = photoDto.Description;
        existingPhoto.Url = photoDto.Url;

        // Pass the updated photo object to the repository for processing
        var updatedPhoto = await _service.UpdatePhotoAsync(existingPhoto);

        // Check if the update was successful
        if (updatedPhoto == null)
        {
            _loggingService.LogError($"Failed to update photo with ID: {id}");
            return StatusCode(500, new { message = "An error occurred while updating the photo" });
        }

        // Invalidate the cache if necessary
        _cache.Remove($"GetPhoto_{id}");
        _loggingService.LogInformation($"Cache invalidated for photo ID: {id}");

        // Return the updated photo data
        return Ok(updatedPhoto);
    }


    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePhoto(int id)
    {
        _loggingService.LogInformation($"Attempting to delete photo with ID: {id}");
        await _service.DeletePhotoAsync(id);
        _loggingService.LogInformation($"Successfully deleted photo with ID: {id}");
        return NoContent();
    }
}
