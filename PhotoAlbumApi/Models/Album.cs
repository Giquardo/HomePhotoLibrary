using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

    // Foreign key to User
    [Required]
    [ForeignKey("User")]
    public int UserId { get; set; }

    // Navigation property for User
    public User User { get; set; }

    // Soft delete properties
    public bool IsDeleted { get; set; } = false; // Initialize with default value
}