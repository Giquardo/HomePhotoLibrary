using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PhotoAlbumApi.Models;
public class Photo
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int AlbumId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Title { get; set; }

    public DateTime DateUploaded { get; internal set; } = DateTime.Now;

    [MaxLength(500)]
    public string Description { get; set; }

    [ForeignKey("AlbumId")]
    public Album Album { get; set; }

    [MaxLength(10)]
    public string Extension { get; set; }

    [NotMapped]
    public string MimeType => GetMimeType(Extension);

    [Required]
    [MaxLength(255)]
    public string FilePath { get; set; }

    private string GetMimeType(string extension)
    {
        return extension.ToLower() switch
        {
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".tiff" => "image/tiff",
            _ => "application/octet-stream", // Default MIME type
        };
    }
}
