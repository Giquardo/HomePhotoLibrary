using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using PhotoAlbumApi.Models;
using PhotoAlbumApi.Repositories;
using PhotoAlbumApi.Validators;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PhotoAlbumApi.Controllers
{
    [ApiController]
    [Route("api/photos")]
    public class PhotoController : ControllerBase
    {
        private readonly IPhotoRepository _photoRepository;
        private readonly LoggingService _loggingService;

        public PhotoController(IPhotoRepository photoRepository, LoggingService loggingService, IValidator<Photo> validator)
        {
            _photoRepository = photoRepository;
            _loggingService = loggingService;
        }

        // GET: api/Photos
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Photo>>> GetPhotos()
        {
            _loggingService.LogInformation("Fetching all photos");
            var photos = await _photoRepository.GetPhotosAsync();
            _loggingService.LogInformation("Successfully fetched all photos");
            return Ok(photos);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Photo>> GetPhoto(int id)
        {
            _loggingService.LogInformation($"Fetching photo with ID: {id}");
            var photo = await _photoRepository.GetPhotoByIdAsync(id);
            if (photo == null)
            {
                _loggingService.LogWarning($"Photo with ID: {id} not found");
                return NotFound();
            }
            _loggingService.LogInformation($"Successfully fetched photo with ID: {id}");
            return Ok(photo);
        }

        [HttpPost]
        public async Task<ActionResult<Photo>> AddPhoto(Photo photo)
        {
            _loggingService.LogInformation("Adding a new photo");
            try
            {
                var addedPhoto = await _photoRepository.AddPhotoAsync(photo);
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
        public async Task<IActionResult> UpdatePhoto(int id, Photo photo)
        {
            _loggingService.LogInformation($"Updating photo with ID: {id}");
            if (id != photo.Id)
            {
                _loggingService.LogWarning("Photo ID mismatch");
                return BadRequest();
            }

            var updatedPhoto = await _photoRepository.UpdatePhotoAsync(photo);
            if (updatedPhoto == null)
            {
                _loggingService.LogWarning($"Photo with ID: {id} not found");
                return NotFound();
            }

            _loggingService.LogInformation($"Successfully updated photo with ID: {id}");
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePhoto(int id)
        {
            _loggingService.LogInformation($"Attempting to delete photo with ID: {id}");
            await _photoRepository.DeletePhotoAsync(id);
            _loggingService.LogInformation($"Successfully deleted photo with ID: {id}");
            return NoContent();
        }
    }
}