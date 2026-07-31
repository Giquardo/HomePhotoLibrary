using Microsoft.AspNetCore.Mvc;
using PhotoAlbumApi.Services;
using PhotoAlbumApi.DTOs;
using Microsoft.AspNetCore.Authorization;

namespace PhotoAlbumApi.Controllers;

[ApiController]
[Route("api/storage")]
[Authorize(Roles = "Admin")]
public class StorageController : ControllerBase
{
    private readonly IStorageUsageService _storageUsageService;

    public StorageController(IStorageUsageService storageUsageService)
    {
        _storageUsageService = storageUsageService;
    }

    [HttpGet]
    public async Task<IActionResult> GetStorageUsage()
    {
        var photoUsage = await _storageUsageService.GetPhotoStorageUsageAsync();
        var backupUsage = await _storageUsageService.GetBackupStorageUsageAsync();

        return Ok(new StorageUsageDto
        {
            PhotoStorageBytes = photoUsage.TotalBytes,
            PhotoStorageFileCount = photoUsage.FileCount,
            BackupStorageBytes = backupUsage.TotalBytes,
            BackupStorageFileCount = backupUsage.FileCount
        });
    }
}
