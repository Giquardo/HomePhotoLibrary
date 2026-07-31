using Microsoft.Extensions.Configuration;
using PhotoAlbumApi.Services;

namespace PhotoAlbumApi.Tests
{
    public class StorageUsageServiceTests : IDisposable
    {
        private readonly string _photoDir = Path.Combine(Path.GetTempPath(), "StorageUsageTests_Photos_" + Guid.NewGuid());
        private readonly string _backupDir = Path.Combine(Path.GetTempPath(), "StorageUsageTests_Backups_" + Guid.NewGuid());

        public void Dispose()
        {
            if (Directory.Exists(_photoDir))
            {
                Directory.Delete(_photoDir, recursive: true);
            }
            if (Directory.Exists(_backupDir))
            {
                Directory.Delete(_backupDir, recursive: true);
            }
        }

        private StorageUsageService CreateService()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Storage:BasePath"] = _photoDir,
                    ["Backup:BasePath"] = _backupDir
                })
                .Build();

            return new StorageUsageService(configuration);
        }

        [Fact]
        public async Task GetPhotoStorageUsageAsync_SumsBytesAndCountsFiles()
        {
            Directory.CreateDirectory(_photoDir);
            File.WriteAllBytes(Path.Combine(_photoDir, "a.jpg"), new byte[100]);
            File.WriteAllBytes(Path.Combine(_photoDir, "b.png"), new byte[250]);

            var service = CreateService();
            var usage = await service.GetPhotoStorageUsageAsync();

            Assert.Equal(350, usage.TotalBytes);
            Assert.Equal(2, usage.FileCount);
        }

        [Fact]
        public async Task GetBackupStorageUsageAsync_SumsBytesAndCountsFiles()
        {
            Directory.CreateDirectory(_backupDir);
            File.WriteAllBytes(Path.Combine(_backupDir, "db_20260101_000000.sql.gz"), new byte[500]);
            File.WriteAllBytes(Path.Combine(_backupDir, "images_20260101_000000.tar.gz"), new byte[1500]);

            var service = CreateService();
            var usage = await service.GetBackupStorageUsageAsync();

            Assert.Equal(2000, usage.TotalBytes);
            Assert.Equal(2, usage.FileCount);
        }

        [Fact]
        public async Task GetPhotoStorageUsageAsync_MissingDirectory_ReturnsZeroInsteadOfThrowing()
        {
            // _photoDir is never created for this test.
            var service = CreateService();
            var usage = await service.GetPhotoStorageUsageAsync();

            Assert.Equal(0, usage.TotalBytes);
            Assert.Equal(0, usage.FileCount);
        }

        [Fact]
        public async Task GetBackupStorageUsageAsync_MissingDirectory_ReturnsZeroInsteadOfThrowing()
        {
            // _backupDir is never created for this test - mirrors the real read-only
            // mount potentially not existing yet on a fresh setup.
            var service = CreateService();
            var usage = await service.GetBackupStorageUsageAsync();

            Assert.Equal(0, usage.TotalBytes);
            Assert.Equal(0, usage.FileCount);
        }
    }
}
