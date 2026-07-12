using System.Net;
using PhotoAlbumApi.Services;

namespace PhotoAlbumApi.Tests
{
    public class ImageServiceTests
    {
        [Theory]
        [InlineData(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }, "jpg")]
        [InlineData(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, "png")]
        [InlineData(new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 }, "gif")]
        [InlineData(new byte[] { 0x47, 0x49, 0x46, 0x38, 0x37, 0x61 }, "gif")]
        [InlineData(new byte[] { 0x42, 0x4D, 0x00, 0x00 }, "bmp")]
        public void DetectImageExtension_RecognizedSignature_ReturnsExpectedExtension(byte[] header, string expected)
        {
            Assert.Equal(expected, ImageService.DetectImageExtension(header));
        }

        [Fact]
        public void DetectImageExtension_WebpSignature_ReturnsWebp()
        {
            var header = new byte[] { 0x52, 0x49, 0x46, 0x46, 0, 0, 0, 0, 0x57, 0x45, 0x42, 0x50 };
            Assert.Equal("webp", ImageService.DetectImageExtension(header));
        }

        [Theory]
        [InlineData(new byte[] { 0x00, 0x01, 0x02, 0x03 })]
        [InlineData(new byte[] { })]
        [InlineData(new byte[] { 0x25, 0x50, 0x44, 0x46 })] // "%PDF"
        public void DetectImageExtension_UnrecognizedSignature_ReturnsNull(byte[] header)
        {
            Assert.Null(ImageService.DetectImageExtension(header));
        }

        [Theory]
        [InlineData("127.0.0.1")]
        [InlineData("10.0.0.5")]
        [InlineData("172.16.0.1")]
        [InlineData("192.168.1.1")]
        [InlineData("169.254.169.254")] // cloud metadata endpoint
        [InlineData("100.64.0.1")]
        [InlineData("0.0.0.0")]
        [InlineData("::1")]
        [InlineData("fe80::1")]
        [InlineData("fc00::1")]
        public void IsPrivateOrReservedAddress_BlockedRanges_ReturnsTrue(string ip)
        {
            Assert.True(ImageService.IsPrivateOrReservedAddress(IPAddress.Parse(ip)));
        }

        [Theory]
        [InlineData("8.8.8.8")]
        [InlineData("93.184.216.34")]
        public void IsPrivateOrReservedAddress_PublicAddress_ReturnsFalse(string ip)
        {
            Assert.False(ImageService.IsPrivateOrReservedAddress(IPAddress.Parse(ip)));
        }
    }
}
