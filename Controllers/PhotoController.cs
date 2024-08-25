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


namespace PhotoAlbumApi.Controllers
{
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
        public async Task<ActionResult<IEnumerable<Photo>>> GetPhotos()
        {
            _loggingService.LogInformation("Fetching all photos");
            var photos = await _service.GetPhotosAsync();
            var photoDtos = _mapper.Map<IEnumerable<PhotoDto>>(photos);
            _loggingService.LogInformation("Successfully fetched all photos");
            return Ok(photoDtos);
        }

        // [HttpGet("{id}")]
        // public async Task<ActionResult<Photo>> GetPhoto(int id)
        // {
        //     _loggingService.LogInformation($"Fetching photo with ID: {id}");
        //     var photo = await _photoRepository.GetPhotoByIdAsync(id);
        //     if (photo == null)
        //     {
        //         _loggingService.LogWarning($"Photo with ID: {id} not found");
        //         return NotFound();
        //     }
        //     _loggingService.LogInformation($"Successfully fetched photo with ID: {id}");
        //     return Ok(photo);
        // }

        // [HttpPost]
        // public async Task<ActionResult<Photo>> AddPhoto(Photo photo)
        // {
        //     _loggingService.LogInformation("Adding a new photo");
        //     try
        //     {
        //         var addedPhoto = await _photoRepository.AddPhotoAsync(photo);
        //         _loggingService.LogInformation($"Successfully added a new photo with ID: {addedPhoto.Id}");
        //         return CreatedAtAction(nameof(GetPhoto), new { id = addedPhoto.Id }, addedPhoto);
        //     }
        //     catch (InvalidOperationException ex)
        //     {
        //         _loggingService.LogError(ex, "Error adding photo");
        //         return Conflict(new { message = ex.Message });
        //     }
        // }

        // [HttpPut("{id}")]
        // public async Task<IActionResult> UpdatePhoto(int id, Photo photo)
        // {
        //     _loggingService.LogInformation($"Updating photo with ID: {id}");
        //     if (id != photo.Id)
        //     {
        //         _loggingService.LogWarning("Photo ID mismatch");
        //         return BadRequest();
        //     }

        //     var updatedPhoto = await _photoRepository.UpdatePhotoAsync(photo);
        //     if (updatedPhoto == null)
        //     {
        //         _loggingService.LogWarning($"Photo with ID: {id} not found");
        //         return NotFound();
        //     }

        //     _loggingService.LogInformation($"Successfully updated photo with ID: {id}");
        //     return NoContent();
        // }

        // [HttpDelete("{id}")]
        // public async Task<IActionResult> DeletePhoto(int id)
        // {
        //     _loggingService.LogInformation($"Attempting to delete photo with ID: {id}");
        //     await _photoRepository.DeletePhotoAsync(id);
        //     _loggingService.LogInformation($"Successfully deleted photo with ID: {id}");
        //     return NoContent();
        // }
    }
}