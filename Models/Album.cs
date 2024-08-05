using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PhotoAlbumApi.Models;
public class Album
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Title { get; set; }

    [MaxLength(500)]
    public string Description { get; set; }

    // Navigation property
    public IList<Photo> Photos { get; set; }
}
