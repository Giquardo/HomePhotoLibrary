using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Security.Cryptography;

namespace PhotoAlbumApi.Models
{
    public class Photo
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int AlbumId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Title { get; set; } = string.Empty;

        public DateTime DateUploaded { get; internal set; } = DateTime.Now;

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        [ForeignKey("AlbumId")]
        public Album Album { get; set; } // Removed initialization here

        [MaxLength(10)]
        public string Extension { get; set; } = string.Empty;

        [NotMapped]
        public string MimeType => GetMimeType(Extension);

        [Required]
        [MaxLength(255)]
        public string FilePath { get; set; } = string.Empty;

        [MaxLength(2083)]
        public string Url { get; set; } = string.Empty;

        [Required]
        [MaxLength(64)]
        public string Hash { get; set; } = string.Empty;

        private string GetMimeType(string extension)
        {
            return extension.ToLower() switch
            {
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                _ => throw new InvalidOperationException("Invalid file extension. Only .jpeg, .jpg, and .png are allowed.")
            };
        }

        public void ComputeHash()
        {
            using (var sha256 = SHA256.Create())
            {
                using (var stream = File.OpenRead(FilePath))
                {
                    var hashBytes = sha256.ComputeHash(stream);
                    Hash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                }
            }
        }
    }
}