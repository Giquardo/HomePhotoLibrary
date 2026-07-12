using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PhotoAlbumApi.Models;
public class ShareLink
{
    [Key]
    public int Id { get; set; }

    // Cryptographically random, unique - this token is the entire access
    // control for the link, so it must never be guessable/sequential.
    [Required]
    [MaxLength(64)]
    public string Token { get; set; } = string.Empty;

    [Required]
    public int AlbumId { get; set; }

    [Required]
    public int CreatedByUserId { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime ExpiresAtUtc { get; set; }

    public DateTime? RevokedAtUtc { get; set; }

    [ForeignKey("AlbumId")]
    public Album Album { get; set; }
}
