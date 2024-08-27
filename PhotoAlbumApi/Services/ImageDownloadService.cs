using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace PhotoAlbumApi.Services;
public interface IImageService
{
    Task<string> DownloadImageAsync(string imageUrl);
    Task<string> SaveUploadedFileAsync(IFormFile file);
}

public class ImageService : IImageService
{
    public async Task<string> DownloadImageAsync(string imageUrl)
    {
        using (HttpClient client = new HttpClient())
        {
            HttpResponseMessage response = await client.GetAsync(imageUrl);
            response.EnsureSuccessStatusCode();

            byte[] imageBytes = await response.Content.ReadAsByteArrayAsync();
            var fileName = Path.GetFileName(new Uri(imageUrl).AbsolutePath);
            var filePath = GenerateFilePath(fileName);

            await File.WriteAllBytesAsync(filePath, imageBytes);
            return filePath;
        }
    }

    public async Task<string> SaveUploadedFileAsync(IFormFile file)
    {
        var fileName = Path.GetFileName(file.FileName);
        var filePath = GenerateFilePath(fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return filePath;
    }

    private string GenerateFilePath(string fileName)
    {
        var directoryPath = Path.Combine("Data", "Files");
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        return Path.Combine(directoryPath, fileName);
    }
}
