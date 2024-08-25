namespace PhotoAlbumApi.DTOs;

public class PhotoDto
{
    public int? Id { get; set; }

    public required int AlbumId { get; set; }

    public required string Title { get; set; }

    public DateTime DateUploaded { get; set; } 

    public string Description { get; set; } = string.Empty;

    public string Extension { get; set; } = string.Empty;

    public string FilePath { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public string Hash { get; set; } = string.Empty;
}
