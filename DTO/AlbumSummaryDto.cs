using PhotoAlbumApi.Models;

namespace PhotoAlbumApi.DTOs;

public class AlbumSummaryDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty; // Initialize with default value
    public string Description { get; set; } = string.Empty; // Initialize with default value
    public IList<Photo> Photos { get; set; } = new List<Photo>(); // Initialize with default value
    public int UserId { get; set; }
}