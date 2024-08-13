using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

        [MaxLength(10)]
        public string Extension { get; set; } = string.Empty;

        [MaxLength(255)]
        public string FilePath { get; set; } = string.Empty;

        [MaxLength(2083)]
        public string Url { get; set; } = string.Empty;

        [MaxLength(64)]
        public string Hash { get; set; } = string.Empty;

    }
}