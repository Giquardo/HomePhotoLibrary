namespace PhotoAlbumApi.DTOs;

public class AlbumDto
{
    public int? Id { get; set; }

    public required string Title { get; set; } 
    public string Description { get; set; } = string.Empty; 
}
