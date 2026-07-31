namespace PhotoAlbumApi.Services;

public record DirectoryUsage(long TotalBytes, int FileCount);

public interface IStorageUsageService
{
    Task<DirectoryUsage> GetPhotoStorageUsageAsync();
    Task<DirectoryUsage> GetBackupStorageUsageAsync();
}

public class StorageUsageService : IStorageUsageService
{
    private readonly string _photoStoragePath;
    private readonly string _backupStoragePath;

    public StorageUsageService(IConfiguration configuration)
    {
        _photoStoragePath = configuration["Storage:BasePath"] ?? Path.Combine("Data", "Files");
        _backupStoragePath = configuration["Backup:BasePath"] ?? "backups";
    }

    public Task<DirectoryUsage> GetPhotoStorageUsageAsync() => GetDirectoryUsageAsync(_photoStoragePath);

    public Task<DirectoryUsage> GetBackupStorageUsageAsync() => GetDirectoryUsageAsync(_backupStoragePath);

    // A missing directory (e.g. no backups taken yet, or the read-only mount
    // isn't present in a non-compose dev setup) is reported as empty rather
    // than an error - the dashboard should never fail just because one of the
    // two directories doesn't exist yet.
    private static Task<DirectoryUsage> GetDirectoryUsageAsync(string path)
    {
        return Task.Run(() =>
        {
            if (!Directory.Exists(path))
            {
                return new DirectoryUsage(0, 0);
            }

            long totalBytes = 0;
            var fileCount = 0;
            foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
            {
                totalBytes += new FileInfo(file).Length;
                fileCount++;
            }

            return new DirectoryUsage(totalBytes, fileCount);
        });
    }
}
