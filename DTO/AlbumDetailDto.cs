namespace PhotoAlbumApi.DTOs;
public class AlbumDetailDto
{
    public int? Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; } = string.Empty; // Initialize with default value
}