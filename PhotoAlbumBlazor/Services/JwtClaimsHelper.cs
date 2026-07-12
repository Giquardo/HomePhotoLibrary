using System.Text;
using System.Text.Json;

namespace PhotoAlbumBlazor.Services;

// Decodes the JWT payload client-side to read claims for UX purposes only
// (e.g. hiding the Users nav link from non-admins). This is NOT a security
// boundary - the token isn't verified here, just read. The real enforcement
// is the API's [Authorize(Roles="Admin")], which always applies regardless
// of what the client shows or hides.
public static class JwtClaimsHelper
{
    public static bool IsAdmin(string? token)
    {
        var claims = DecodeClaims(token);
        return claims.HasValue &&
               claims.Value.TryGetProperty("IsAdmin", out var value) &&
               string.Equals(value.GetString(), "True", StringComparison.OrdinalIgnoreCase);
    }

    public static int? GetUserId(string? token)
    {
        var claims = DecodeClaims(token);
        if (claims.HasValue && claims.Value.TryGetProperty("UserId", out var value) &&
            int.TryParse(value.GetString(), out var userId))
        {
            return userId;
        }
        return null;
    }

    private static JsonElement? DecodeClaims(string? token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return null;
        }

        var parts = token.Split('.');
        if (parts.Length != 3)
        {
            return null;
        }

        try
        {
            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            switch (payload.Length % 4)
            {
                case 2: payload += "=="; break;
                case 3: payload += "="; break;
            }

            var bytes = Convert.FromBase64String(payload);
            return JsonDocument.Parse(Encoding.UTF8.GetString(bytes)).RootElement;
        }
        catch
        {
            return null;
        }
    }
}
