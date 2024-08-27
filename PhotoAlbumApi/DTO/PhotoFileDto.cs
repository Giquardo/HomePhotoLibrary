using PhotoAlbumApi.Models;

namespace PhotoAlbumApi.DTOs
{
    public class PhotoFileDto
    {
        public Photo Photo { get; set; }
        public byte[] FileData { get; set; }
        public string FileName { get; set; }
        public string ContentType { get; set; }
    }
}
