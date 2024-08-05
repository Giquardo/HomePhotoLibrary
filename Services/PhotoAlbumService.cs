using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PhotoAlbumApi.Models;
using PhotoAlbumApi.Repositories;

namespace PhotoAlbumApi.Services
{
    public interface IPhotoAlbumService
    {
        Task<IEnumerable<Album>> GetAlbumsAsync();
        Task<Album?> GetAlbumAsync(int id);
        Task<Album> AddAlbumAsync(Album album);
        Task<Album?> UpdateAlbumAsync(Album album);
        Task DeleteAlbumAsync(int id);
    }

    public class PhotoAlbumService : IPhotoAlbumService
    {
        private readonly IAlbumRepository _albumRepository;

        public PhotoAlbumService(IAlbumRepository albumRepository)
        {
            _albumRepository = albumRepository;
        }

        public async Task<IEnumerable<Album>> GetAlbumsAsync()
        {
            return await _albumRepository.GetAlbumsAsync();
        }

        public async Task<Album?> GetAlbumAsync(int id)
        {
            return await _albumRepository.GetAlbumAsync(id);
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
    }
}