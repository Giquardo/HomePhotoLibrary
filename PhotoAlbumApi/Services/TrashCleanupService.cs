using PhotoAlbumApi.Repositories;

namespace PhotoAlbumApi.Services;

public interface ITrashCleanupService
{
    Task<(int albumsPurged, int photosPurged)> PurgeExpiredTrashAsync(DateTime utcNow);
}

public class TrashCleanupService : ITrashCleanupService
{
    private const int DefaultRetentionDays = 30;

    private readonly IAlbumRepository _albumRepository;
    private readonly IPhotoRepository _photoRepository;
    private readonly IImageService _imageService;
    private readonly ILoggingService _loggingService;
    private readonly IConfiguration _configuration;

    public TrashCleanupService(
        IAlbumRepository albumRepository,
        IPhotoRepository photoRepository,
        IImageService imageService,
        ILoggingService loggingService,
        IConfiguration configuration)
    {
        _albumRepository = albumRepository;
        _photoRepository = photoRepository;
        _imageService = imageService;
        _loggingService = loggingService;
        _configuration = configuration;
    }

    public async Task<(int albumsPurged, int photosPurged)> PurgeExpiredTrashAsync(DateTime utcNow)
    {
        var retentionDays = _configuration.GetValue<int?>("Trash:RetentionDays") ?? DefaultRetentionDays;
        var cutoffUtc = utcNow - TimeSpan.FromDays(retentionDays);

        var albumsPurged = 0;
        var photosPurged = 0;

        // Albums first: purging an album takes every photo under it with it
        // (DB cascade), regardless of each photo's own IsDeleted flag.
        var albums = await _albumRepository.GetAlbumsPendingPurgeAsync(cutoffUtc);
        foreach (var album in albums)
        {
            foreach (var photo in album.Photos)
            {
                _imageService.DeleteFile(photo.FilePath);
            }

            await _albumRepository.PurgeAlbumAsync(album);
            albumsPurged++;
        }

        // Standalone photos: individually soft-deleted, past the cutoff, whose
        // album isn't itself pending purge (that case is handled above).
        var photos = await _photoRepository.GetPhotosPendingPurgeAsync(cutoffUtc);
        foreach (var photo in photos)
        {
            _imageService.DeleteFile(photo.FilePath);
            await _photoRepository.PurgePhotoAsync(photo);
            photosPurged++;
        }

        _loggingService.LogInformation(
            $"Trash purge complete: {albumsPurged} album(s) and {photosPurged} standalone photo(s) permanently deleted (retention: {retentionDays} days).");

        return (albumsPurged, photosPurged);
    }
}
