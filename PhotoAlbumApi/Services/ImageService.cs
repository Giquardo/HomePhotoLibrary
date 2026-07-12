using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using Microsoft.AspNetCore.Http;

namespace PhotoAlbumApi.Services;
public interface IImageService
{
    Task<string> DownloadImageAsync(string imageUrl);
    Task<string> SaveUploadedFileAsync(IFormFile file);
}

public class ImageService : IImageService
{
    private const long MaxUploadSizeBytes = 20 * 1024 * 1024; // 20 MB
    private const int MaxRedirects = 3;
    public const string DownloadHttpClientName = "ImageDownload";

    private readonly string _filesBasePath;
    private readonly IHttpClientFactory _httpClientFactory;

    public ImageService(IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        _filesBasePath = configuration["Storage:BasePath"] ?? Path.Combine("Data", "Files");
        _httpClientFactory = httpClientFactory;
    }

    public async Task<string> SaveUploadedFileAsync(IFormFile file)
    {
        if (file.Length > MaxUploadSizeBytes)
        {
            throw new InvalidOperationException("Uploaded file exceeds the maximum allowed size.");
        }

        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream);
        return await SaveImageBytesAsync(memoryStream.ToArray());
    }

    public async Task<string> DownloadImageAsync(string imageUrl)
    {
        if (!Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException("Only http/https URLs are allowed.");
        }

        var client = _httpClientFactory.CreateClient(DownloadHttpClientName);
        var currentUri = uri;

        for (var redirectCount = 0; ; redirectCount++)
        {
            await EnsureHostIsAllowedAsync(currentUri);

            using var request = new HttpRequestMessage(HttpMethod.Get, currentUri);
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            if (IsRedirect(response.StatusCode))
            {
                if (redirectCount >= MaxRedirects || response.Headers.Location == null)
                {
                    throw new InvalidOperationException("The image URL redirected too many times or had no redirect target.");
                }

                currentUri = response.Headers.Location.IsAbsoluteUri
                    ? response.Headers.Location
                    : new Uri(currentUri, response.Headers.Location);

                if (currentUri.Scheme != Uri.UriSchemeHttp && currentUri.Scheme != Uri.UriSchemeHttps)
                {
                    throw new InvalidOperationException("The image URL redirected to a disallowed scheme.");
                }

                continue;
            }

            response.EnsureSuccessStatusCode();
            var imageBytes = await ReadBoundedAsync(response.Content);
            return await SaveImageBytesAsync(imageBytes);
        }
    }

    private async Task<string> SaveImageBytesAsync(byte[] imageBytes)
    {
        var extension = DetectImageExtension(imageBytes);
        if (extension == null)
        {
            throw new InvalidOperationException("The file is not a recognized image type.");
        }

        if (!Directory.Exists(_filesBasePath))
        {
            Directory.CreateDirectory(_filesBasePath);
        }

        var fileName = $"{Guid.NewGuid():N}.{extension}";
        var filePath = Path.Combine(_filesBasePath, fileName);
        await File.WriteAllBytesAsync(filePath, imageBytes);
        return filePath;
    }

    private static async Task<byte[]> ReadBoundedAsync(HttpContent content)
    {
        await using var stream = await content.ReadAsStreamAsync();
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        long total = 0;
        int read;
        while ((read = await stream.ReadAsync(chunk)) > 0)
        {
            total += read;
            if (total > MaxUploadSizeBytes)
            {
                throw new InvalidOperationException("Downloaded image exceeds the maximum allowed size.");
            }
            buffer.Write(chunk, 0, read);
        }
        return buffer.ToArray();
    }

    private static bool IsRedirect(HttpStatusCode statusCode) => statusCode is
        HttpStatusCode.Moved or HttpStatusCode.Found or HttpStatusCode.SeeOther or
        HttpStatusCode.TemporaryRedirect or HttpStatusCode.PermanentRedirect;

    private static async Task EnsureHostIsAllowedAsync(Uri uri)
    {
        IPAddress[] addresses;
        try
        {
            addresses = await Dns.GetHostAddressesAsync(uri.Host);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Could not resolve host '{uri.Host}'.", ex);
        }

        if (addresses.Length == 0 || addresses.Any(IsPrivateOrReservedAddress))
        {
            throw new InvalidOperationException("The target host resolves to a private, loopback, or reserved address and is not allowed.");
        }
    }

    // Blocks RFC1918/loopback/link-local/CGNAT/reserved ranges to prevent SSRF
    // against internal services (routers, other containers, cloud metadata).
    public static bool IsPrivateOrReservedAddress(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            var b = address.GetAddressBytes();
            return b[0] == 0                                            // 0.0.0.0/8
                || b[0] == 10                                           // 10.0.0.0/8
                || (b[0] == 100 && b[1] >= 64 && b[1] <= 127)           // 100.64.0.0/10 CGNAT
                || b[0] == 127                                          // 127.0.0.0/8 loopback
                || (b[0] == 169 && b[1] == 254)                        // 169.254.0.0/16 link-local
                || (b[0] == 172 && b[1] >= 16 && b[1] <= 31)            // 172.16.0.0/12
                || (b[0] == 192 && b[1] == 168)                        // 192.168.0.0/16
                || b[0] >= 224;                                         // multicast/reserved
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            if (IPAddress.IsLoopback(address))
            {
                return true; // ::1
            }
            var b = address.GetAddressBytes();
            if (b[0] == 0xFE && (b[1] & 0xC0) == 0x80)
            {
                return true; // fe80::/10 link-local
            }
            if ((b[0] & 0xFE) == 0xFC)
            {
                return true; // fc00::/7 unique local
            }
            return false;
        }

        return true; // unknown address family: block rather than risk it
    }

    // Identifies the image type from its magic bytes rather than trusting the
    // client-supplied filename/Content-Type, and drives the server-generated
    // filename's extension.
    public static string? DetectImageExtension(byte[] header)
    {
        if (header.Length >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
        {
            return "jpg";
        }

        if (header.Length >= 8 &&
            header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47 &&
            header[4] == 0x0D && header[5] == 0x0A && header[6] == 0x1A && header[7] == 0x0A)
        {
            return "png";
        }

        if (header.Length >= 6 &&
            header[0] == 0x47 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x38 &&
            (header[4] == 0x37 || header[4] == 0x39) && header[5] == 0x61)
        {
            return "gif";
        }

        if (header.Length >= 12 &&
            header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46 &&
            header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50)
        {
            return "webp";
        }

        if (header.Length >= 2 && header[0] == 0x42 && header[1] == 0x4D)
        {
            return "bmp";
        }

        return null;
    }
}
