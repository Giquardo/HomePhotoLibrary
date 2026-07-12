namespace PhotoAlbumApi.DTOs;

public class ShareLinkDto
{
    public string Token { get; set; } = string.Empty;
    public int AlbumId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
}
