using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PhotoAlbumApi.Models;
public class Album
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Title { get; set; } = string.Empty; // Initialize with default value

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty; // Initialize with default value

    // Navigation property
    public IList<Photo> Photos { get; set; } = new List<Photo>(); // Initialize with default value
}
