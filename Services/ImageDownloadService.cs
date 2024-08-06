using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using PhotoAlbumApi.Models;

namespace PhotoAlbumApi.Services;
public class ImageDownloadService
{
    public async Task DownloadImageAsync(Photo photo)
    {
        if (string.IsNullOrEmpty(photo.Url))
        {
            throw new ArgumentException("URL cannot be null or empty", nameof(photo.Url));
        }

        using (var httpClient = new HttpClient())
        {
            var response = await httpClient.GetAsync(photo.Url);
            response.EnsureSuccessStatusCode();

            var fileName = Path.GetFileName(photo.Url);
            var relativePath = Path.Combine("Data", "Files");
            var filePath = Path.Combine(relativePath, fileName);

            Directory.CreateDirectory(relativePath);

            using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await response.Content.CopyToAsync(fileStream);
            }

            photo.FilePath = filePath;
            photo.Extension = Path.GetExtension(filePath);
            photo.ComputeHash();
        }
    }
}
