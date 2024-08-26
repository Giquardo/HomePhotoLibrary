namespace PhotoAlbumApi.DTOs;

public class UpdateAlbumDto
{
    public required string Title { get; set; }
    public string Description { get; set; } = string.Empty;
}
