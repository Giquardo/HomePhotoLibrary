
namespace PhotoAlbumApi.DTOs;

public class PhotoDisplayDto
{
    public int Id { get; set; }
    public int AlbumId { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string Url { get; set; }
    public string Extension { get; set; }
}