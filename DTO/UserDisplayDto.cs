namespace PhotoAlbumApi.DTOs;
public class UserDisplayDto
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public bool IsAdmin { get; set; }
    public ICollection<AlbumDto> Albums { get; set; } = new List<AlbumDto>();
}
