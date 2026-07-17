

namespace PhotoAlbumApi.DTOs;

public class PhotoUploadDto
{
    // Non-null by the time controller logic runs: FluentValidation's NotNull rule
    // + [ApiController] auto-validation reject the request first if it's missing.
    public IFormFile File { get; set; } = null!;
    public int AlbumId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
}