

namespace PhotoAlbumApi.DTOs;

public class PhotoUploadDto
{
    public IFormFile File { get; set; }
    public int AlbumId { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
}