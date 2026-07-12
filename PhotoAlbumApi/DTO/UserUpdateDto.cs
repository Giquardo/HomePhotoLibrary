namespace PhotoAlbumApi.DTOs;

public class UserUpdateDto
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    // Optional: leaving this blank keeps the existing password hash. Unlike
    // UserDto (create), update has no way to know the current plaintext
    // password, so it can't be "required" the same way.
    public string? Password { get; set; }

    public bool IsAdmin { get; set; } = false;
}
