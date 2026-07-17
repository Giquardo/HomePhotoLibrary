namespace PhotoAlbumApi.DTOs;
public class UserDisplayDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
    public ICollection<AlbumDto> Albums { get; set; } = new List<AlbumDto>();
}
