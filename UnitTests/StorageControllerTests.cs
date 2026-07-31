using Moq;
using Microsoft.AspNetCore.Mvc;
using PhotoAlbumApi.Controllers;
using PhotoAlbumApi.Services;
using PhotoAlbumApi.DTOs;

namespace PhotoAlbumApi.Tests
{
    public class StorageControllerTests
    {
        private readonly Mock<IStorageUsageService> _mockStorageUsageService;
        private readonly StorageController _controller;

        public StorageControllerTests()
        {
            _mockStorageUsageService = new Mock<IStorageUsageService>();
            _controller = new StorageController(_mockStorageUsageService.Object);
        }

        [Fact]
        public async Task GetStorageUsage_ReturnsOkWithCombinedUsage()
        {
            _mockStorageUsageService.Setup(s => s.GetPhotoStorageUsageAsync())
                .ReturnsAsync(new DirectoryUsage(123456, 7));
            _mockStorageUsageService.Setup(s => s.GetBackupStorageUsageAsync())
                .ReturnsAsync(new DirectoryUsage(654321, 3));

            var result = await _controller.GetStorageUsage();

            var okResult = Assert.IsType<OkObjectResult>(result);
            var dto = Assert.IsType<StorageUsageDto>(okResult.Value);
            Assert.Equal(123456, dto.PhotoStorageBytes);
            Assert.Equal(7, dto.PhotoStorageFileCount);
            Assert.Equal(654321, dto.BackupStorageBytes);
            Assert.Equal(3, dto.BackupStorageFileCount);
        }
    }
}
