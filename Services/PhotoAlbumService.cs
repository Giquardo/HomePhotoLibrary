using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PhotoAlbumApi.Models;
using PhotoAlbumApi.Repositories;

namespace PhotoAlbumApi.Services;
public interface IPhotoAlbumService
{
    Task<IEnumerable<Album>> GetAlbumsAsync();
    Task<Album?> GetAlbumAsync(int id);
    Task<Album> AddAlbumAsync(Album album);
    Task<Album?> UpdateAlbumAsync(Album album);
    Task DeleteAlbumAsync(int id);
    Task<Album> UndoDeleteAlbumAsync(int id);
    Task<IEnumerable<Photo>> GetPhotosAsync();
    Task<Photo?> GetPhotoAsync(int id);
    Task<Photo> AddPhotoAsync(Photo photo);
    Task<Photo?> UpdatePhotoAsync(Photo photo);
    Task DeletePhotoAsync(int id);

}

public class PhotoAlbumService : IPhotoAlbumService
{
    private readonly IAlbumRepository _albumRepository;
    private readonly IPhotoRepository _photoRepository;

    public PhotoAlbumService(IAlbumRepository albumRepository, IPhotoRepository photoRepository)
    {
        _albumRepository = albumRepository;
        _photoRepository = photoRepository;
    }

    public async Task<IEnumerable<Album>> GetAlbumsAsync()
    {
        return await _albumRepository.GetAlbumsAsync();
    }

    public async Task<Album?> GetAlbumAsync(int id)
    {
        return await _albumRepository.GetAlbumByIdAsync(id);
    }

    public async Task<Album> AddAlbumAsync(Album album)
    {
        return await _albumRepository.AddAlbumAsync(album);
    }

    public async Task<Album?> UpdateAlbumAsync(Album album)
    {
        return await _albumRepository.UpdateAlbumAsync(album);
    }

    public async Task DeleteAlbumAsync(int id)
    {
        await _albumRepository.DeleteAlbumAsync(id);
    }

    public async Task<IEnumerable<Photo>> GetPhotosAsync()
    {
        return await _photoRepository.GetPhotosAsync();
    }

    public async Task<Photo?> GetPhotoAsync(int id)
    {
        return await _photoRepository.GetPhotoByIdAsync(id);
    }

    public async Task<Photo> AddPhotoAsync(Photo photo)
    {
        return await _photoRepository.AddPhotoAsync(photo);
    }

    public async Task<Photo?> UpdatePhotoAsync(Photo photo)
    {
        return await _photoRepository.UpdatePhotoAsync(photo);
    }

    public async Task DeletePhotoAsync(int id)
    {
        await _photoRepository.DeletePhotoAsync(id);
    }

    public async Task<Album> UndoDeleteAlbumAsync(int id)
    {
        return await _albumRepository.UndoDeleteAlbumAsync(id);
    }



}