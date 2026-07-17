using Microsoft.Extensions.Configuration;
using Moq;
using PhotoAlbumApi.Models;
using PhotoAlbumApi.Repositories;
using PhotoAlbumApi.Services;

namespace PhotoAlbumApi.Tests
{
    public class TrashCleanupServiceTests
    {
        private readonly Mock<IAlbumRepository> _mockAlbumRepository;
        private readonly Mock<IPhotoRepository> _mockPhotoRepository;
        private readonly Mock<IImageService> _mockImageService;
        private readonly Mock<ILoggingService> _mockLoggingService;

        public TrashCleanupServiceTests()
        {
            _mockAlbumRepository = new Mock<IAlbumRepository>();
            _mockPhotoRepository = new Mock<IPhotoRepository>();
            _mockImageService = new Mock<IImageService>();
            _mockLoggingService = new Mock<ILoggingService>();

            _mockAlbumRepository.Setup(r => r.GetAlbumsPendingPurgeAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(new List<Album>());
            _mockPhotoRepository.Setup(r => r.GetPhotosPendingPurgeAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(new List<Photo>());
        }

        private TrashCleanupService CreateService(int? retentionDays = null)
        {
            var settings = new Dictionary<string, string?>();
            if (retentionDays.HasValue)
            {
                settings["Trash:RetentionDays"] = retentionDays.Value.ToString();
            }
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

            return new TrashCleanupService(
                _mockAlbumRepository.Object,
                _mockPhotoRepository.Object,
                _mockImageService.Object,
                _mockLoggingService.Object,
                configuration);
        }

        [Fact]
        public async Task PurgeExpiredTrashAsync_AlbumPendingPurge_DeletesItsPhotoFilesAndPurgesAlbum()
        {
            var album = new Album
            {
                Id = 1,
                IsDeleted = true,
                Photos = new List<Photo>
                {
                    new Photo { Id = 10, FilePath = "/data/files/a.jpg" },
                    new Photo { Id = 11, FilePath = "/data/files/b.jpg" }
                }
            };
            _mockAlbumRepository.Setup(r => r.GetAlbumsPendingPurgeAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(new List<Album> { album });

            var service = CreateService();
            var (albumsPurged, photosPurged) = await service.PurgeExpiredTrashAsync(DateTime.UtcNow);

            Assert.Equal(1, albumsPurged);
            Assert.Equal(0, photosPurged);
            _mockImageService.Verify(s => s.DeleteFile("/data/files/a.jpg"), Times.Once);
            _mockImageService.Verify(s => s.DeleteFile("/data/files/b.jpg"), Times.Once);
            _mockAlbumRepository.Verify(r => r.PurgeAlbumAsync(album), Times.Once);
        }

        [Fact]
        public async Task PurgeExpiredTrashAsync_NoAlbumsPending_NeverPurgesAnAlbum()
        {
            var service = CreateService();
            var (albumsPurged, _) = await service.PurgeExpiredTrashAsync(DateTime.UtcNow);

            Assert.Equal(0, albumsPurged);
            _mockAlbumRepository.Verify(r => r.PurgeAlbumAsync(It.IsAny<Album>()), Times.Never);
            _mockImageService.Verify(s => s.DeleteFile(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task PurgeExpiredTrashAsync_StandalonePhotoPendingPurge_DeletesFileAndPurgesPhoto()
        {
            var photo = new Photo { Id = 20, FilePath = "/data/files/c.jpg", IsDeleted = true };
            _mockPhotoRepository.Setup(r => r.GetPhotosPendingPurgeAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(new List<Photo> { photo });

            var service = CreateService();
            var (albumsPurged, photosPurged) = await service.PurgeExpiredTrashAsync(DateTime.UtcNow);

            Assert.Equal(0, albumsPurged);
            Assert.Equal(1, photosPurged);
            _mockImageService.Verify(s => s.DeleteFile("/data/files/c.jpg"), Times.Once);
            _mockPhotoRepository.Verify(r => r.PurgePhotoAsync(photo), Times.Once);
        }

        [Fact]
        public async Task PurgeExpiredTrashAsync_NoPhotosPending_NeverPurgesAPhoto()
        {
            var service = CreateService();
            var (_, photosPurged) = await service.PurgeExpiredTrashAsync(DateTime.UtcNow);

            Assert.Equal(0, photosPurged);
            _mockPhotoRepository.Verify(r => r.PurgePhotoAsync(It.IsAny<Photo>()), Times.Never);
        }

        [Fact]
        public async Task PurgeExpiredTrashAsync_AlbumsAndPhotosBothPending_ProcessesBothAndReturnsCombinedCounts()
        {
            var album = new Album { Id = 1, IsDeleted = true, Photos = new List<Photo>() };
            var photo = new Photo { Id = 20, FilePath = "/data/files/c.jpg", IsDeleted = true };
            _mockAlbumRepository.Setup(r => r.GetAlbumsPendingPurgeAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(new List<Album> { album });
            _mockPhotoRepository.Setup(r => r.GetPhotosPendingPurgeAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(new List<Photo> { photo });

            var service = CreateService();
            var (albumsPurged, photosPurged) = await service.PurgeExpiredTrashAsync(DateTime.UtcNow);

            Assert.Equal(1, albumsPurged);
            Assert.Equal(1, photosPurged);
        }

        [Fact]
        public async Task PurgeExpiredTrashAsync_DefaultRetention_UsesThirtyDayCutoff()
        {
            var utcNow = new DateTime(2026, 7, 17, 12, 0, 0, DateTimeKind.Utc);
            DateTime? capturedCutoff = null;
            _mockAlbumRepository.Setup(r => r.GetAlbumsPendingPurgeAsync(It.IsAny<DateTime>()))
                .Callback<DateTime>(cutoff => capturedCutoff = cutoff)
                .ReturnsAsync(new List<Album>());

            var service = CreateService(); // no Trash:RetentionDays configured -> default of 30
            await service.PurgeExpiredTrashAsync(utcNow);

            Assert.Equal(utcNow.AddDays(-30), capturedCutoff);
        }

        [Fact]
        public async Task PurgeExpiredTrashAsync_ConfiguredRetention_UsesConfiguredCutoff()
        {
            var utcNow = new DateTime(2026, 7, 17, 12, 0, 0, DateTimeKind.Utc);
            DateTime? capturedCutoff = null;
            _mockPhotoRepository.Setup(r => r.GetPhotosPendingPurgeAsync(It.IsAny<DateTime>()))
                .Callback<DateTime>(cutoff => capturedCutoff = cutoff)
                .ReturnsAsync(new List<Photo>());

            var service = CreateService(retentionDays: 7);
            await service.PurgeExpiredTrashAsync(utcNow);

            Assert.Equal(utcNow.AddDays(-7), capturedCutoff);
        }
    }
}
