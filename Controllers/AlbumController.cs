using Microsoft.AspNetCore.Mvc;
using PhotoAlbumApi.Models;
using PhotoAlbumApi.Services;

namespace PhotoAlbumApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AlbumsController : ControllerBase
    {
        private readonly IPhotoAlbumService _service;

        public AlbumsController(IPhotoAlbumService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAlbums()
        {
            var albums = await _service.GetAlbumsAsync();
            return Ok(albums);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetAlbum(int id)
        {
            var album = await _service.GetAlbumAsync(id);
            if (album is not null)
            {
                return Ok(album);
            }
            else
            {
                return NotFound(new { message = "Album not found", albumId = id });
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddAlbum(Album album)
        {
            var newAlbum = await _service.AddAlbumAsync(album);
            return CreatedAtAction(nameof(GetAlbum), new { id = newAlbum.Id }, newAlbum);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAlbum(int id, Album updatedAlbum)
        {
            var album = await _service.GetAlbumAsync(id);
            if (album is not null)
            {
                album.Title = updatedAlbum.Title;
                album.Description = updatedAlbum.Description;
                await _service.UpdateAlbumAsync(album);
                return Ok(album);
            }
            else
            {
                return NotFound(new { message = "Album not found", albumId = id });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAlbum(int id)
        {
            await _service.DeleteAlbumAsync(id);
            return NoContent();
        }
    }
}