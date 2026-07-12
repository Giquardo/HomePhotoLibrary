namespace PhotoAlbumApi.DTOs;

public class CreateShareLinkDto
{
    public int AlbumId { get; set; }
    public int ExpiresInHours { get; set; } = 168; // 7 days
}
