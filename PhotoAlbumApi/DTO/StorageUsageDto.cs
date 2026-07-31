namespace PhotoAlbumApi.DTOs;

public class StorageUsageDto
{
    public long PhotoStorageBytes { get; set; }
    public int PhotoStorageFileCount { get; set; }
    public long BackupStorageBytes { get; set; }
    public int BackupStorageFileCount { get; set; }
}
