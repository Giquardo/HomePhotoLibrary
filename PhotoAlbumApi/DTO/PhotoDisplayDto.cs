
namespace PhotoAlbumApi.DTOs;

public class PhotoDisplayDto
{
    public int Id { get; set; }
    public int AlbumId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
}