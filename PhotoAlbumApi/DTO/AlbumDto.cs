namespace PhotoAlbumApi.DTOs;

public class AlbumDto
{
    public required string Title { get; set; }
    public string Description { get; set; } = string.Empty;
}
