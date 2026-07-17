using PhotoAlbumApi.Models;

namespace PhotoAlbumApi.DTOs
{
    public class PhotoFileDto
    {
        public Photo Photo { get; set; } = null!;
        public byte[] FileData { get; set; } = Array.Empty<byte>();
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
    }
}
