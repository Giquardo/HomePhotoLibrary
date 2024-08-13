using Microsoft.AspNetCore.Mvc;
using PhotoAlbumApi.Models;
using PhotoAlbumApi.Services;
using AutoMapper;
using System.Collections.Generic;
using System.Threading.Tasks;
using PhotoAlbumApi.DTOs;
using Microsoft.Extensions.Logging;

namespace PhotoAlbumApi.Controllers;

[ApiController]
[Route("api/albums")]
public class AlbumsController : ControllerBase
{
    private readonly IPhotoAlbumService _service;
    private readonly IMapper _mapper;
    private readonly LoggingService _loggingService;

    public AlbumsController(IPhotoAlbumService service, IMapper mapper, LoggingService loggingService)
    {
        _service = service;
        _mapper = mapper;
        _loggingService = loggingService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAlbums()
    {
        _loggingService.LogInformation("Fetching all albums");

        var albums = await _service.GetAlbumsAsync();
        var albumDtos = _mapper.Map<IEnumerable<AlbumDto>>(albums);

        _loggingService.LogInformation("Successfully fetched all albums");

        return Ok(albumDtos);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetAlbum(int id)
    {
        _loggingService.LogInformation($"Fetching album with ID: {id}");

        var album = await _service.GetAlbumAsync(id);
        if (album is not null)
        {
            var albumDto = _mapper.Map<AlbumDto>(album);
            _loggingService.LogInformation($"Successfully fetched album with ID: {id}");
            return Ok(albumDto);
        }
        else
        {
            _loggingService.LogWarning($"Album with ID: {id} not found");
            return NotFound(new { message = "Album not found", albumId = id });
        }
    }

    [HttpPost]
    public async Task<IActionResult> AddAlbum(AlbumDto albumDto)
    {
        _loggingService.LogInformation("Adding a new album");

        var album = _mapper.Map<Album>(albumDto);
        var newAlbum = await _service.AddAlbumAsync(album);
        var newAlbumDto = _mapper.Map<AlbumDto>(newAlbum);

        _loggingService.LogInformation($"Successfully added a new album with ID: {newAlbumDto.Id}");

        return CreatedAtAction(nameof(GetAlbum), new { id = newAlbumDto.Id }, newAlbumDto);
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
            return NoContent();
        }
        else
        {
            _loggingService.LogWarning($"Album with ID: {id} not found");
            return NotFound(new { message = "Album not found", albumId = id });
        }
    }
}