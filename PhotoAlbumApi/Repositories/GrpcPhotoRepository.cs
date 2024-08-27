using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PhotoAlbumApi.Models;
using PhotoAlbumApi.Data;

namespace PhotoAlbumApi.Repositories
{
    public interface IGrpcPhotoRepository
    {
        Task<Photo> GetPhotoByIdAsync(int id);
        Task<Photo> AddPhotoAsync(Photo photo);
        Task<Photo?> GetPhotoByHashAsync(string hash);
        Task<Photo> UpdatePhotoAsync(Photo photo);
        Task<Photo> SoftDeletePhotoAsync(int id);
    }

    public class GrpcPhotoRepository : IGrpcPhotoRepository
    {
        private readonly PhotoAlbumContext _context;

        public GrpcPhotoRepository(PhotoAlbumContext context)
        {
            _context = context;
        }

        public async Task<Photo> GetPhotoByIdAsync(int id)
        {
            return await _context.Photos
                .Where(p => p.Id == id && !p.IsDeleted)
                .FirstOrDefaultAsync();
        }

        public async Task<Photo> AddPhotoAsync(Photo photo)
        {
            _context.Photos.Add(photo);
            await _context.SaveChangesAsync();
            return photo;
        }

        public async Task<Photo?> GetPhotoByHashAsync(string hash)
        {
            return await _context.Photos.FirstOrDefaultAsync(p => p.Hash == hash && !p.IsDeleted);
        }

        public async Task<Photo> UpdatePhotoAsync(Photo photo)
        {
            var existingPhoto = await _context.Photos
               .Where(p => p.Id == photo.Id && !p.IsDeleted)
               .FirstOrDefaultAsync();

            if (existingPhoto == null)
            {
                return null; // Or throw an exception if preferred
            }

            existingPhoto.AlbumId = photo.AlbumId;
            existingPhoto.Title = photo.Title;
            existingPhoto.Description = photo.Description;

            _context.Photos.Update(existingPhoto);
            await _context.SaveChangesAsync();
            return existingPhoto;
        }

        public async Task<Photo> SoftDeletePhotoAsync(int id)
        {
            var photo = await _context.Photos
                .Where(p => p.Id == id && !p.IsDeleted)
                .FirstOrDefaultAsync();

            if (photo == null)
            {
                return null; // Or throw an exception if preferred
            }

            photo.IsDeleted = true;
            _context.Photos.Update(photo);
            await _context.SaveChangesAsync();
            return photo;
        }
    }
}