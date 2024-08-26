using System.ComponentModel.DataAnnotations;

namespace PhotoAlbumApi.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Password { get; set; } = string.Empty;

        public bool IsAdmin { get; set; }

        // Navigation properties
        public ICollection<Album> Albums { get; set; } = new List<Album>();
    }
}