namespace PhotoAlbumApi.DTOs;

public class SharedAlbumDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<SharedPhotoDto> Photos { get; set; } = new();
}

public class SharedPhotoDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
}
