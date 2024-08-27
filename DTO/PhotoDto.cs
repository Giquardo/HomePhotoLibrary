using System.ComponentModel.DataAnnotations;

namespace PhotoAlbumApi.DTOs;

public class PhotoDto
{
    [Required]
    public int AlbumId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(2083)]
    public string Url { get; set; } = string.Empty;

}