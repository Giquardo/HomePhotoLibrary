using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Moq;
using PhotoAlbumApi.Services;

namespace PhotoAlbumApi.Tests
{
    public class ImageServiceTests : IDisposable
    {
        // A real 1x1 transparent PNG - used wherever a test needs actual
        // valid image bytes (magic-byte detection is content-based, not
        // extension/Content-Type based, so it must be real).
        private static readonly byte[] ValidPngBytes = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");

        private readonly string _tempDir = Path.Combine(Path.GetTempPath(), "ImageServiceTests_" + Guid.NewGuid());

        public void Dispose()
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, recursive: true);
            }
        }

        private ImageService CreateService(HttpMessageHandler? handler = null)
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["Storage:BasePath"] = _tempDir })
                .Build();

            var httpClient = new HttpClient(handler ?? new FakeHttpMessageHandler(_ =>
                throw new InvalidOperationException("No HTTP call was expected in this test.")));

            var mockFactory = new Mock<IHttpClientFactory>();
            mockFactory.Setup(f => f.CreateClient(ImageService.DownloadHttpClientName)).Returns(httpClient);

            return new ImageService(configuration, mockFactory.Object);
        }

        private static Mock<IFormFile> CreateMockFormFile(byte[] content)
        {
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.Length).Returns(content.Length);
            mockFile.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Returns<Stream, CancellationToken>((target, token) => new MemoryStream(content).CopyToAsync(target, token));
            return mockFile;
        }

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

        [Fact]
        public async Task SaveUploadedFileAsync_FileExceedsMaxSize_ThrowsAndNeverReadsContent()
        {
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.Length).Returns(21L * 1024 * 1024); // over the 20MB cap

            var service = CreateService();

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.SaveUploadedFileAsync(mockFile.Object));
            mockFile.Verify(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task SaveUploadedFileAsync_ValidPng_SavesWithRandomGuidFilenameAndDetectedExtension()
        {
            var service = CreateService();
            var mockFile = CreateMockFormFile(ValidPngBytes);

            var savedPath = await service.SaveUploadedFileAsync(mockFile.Object);

            Assert.True(File.Exists(savedPath));
            Assert.Equal(".png", Path.GetExtension(savedPath));
            Assert.True(Guid.TryParseExact(Path.GetFileNameWithoutExtension(savedPath), "N", out _),
                $"Expected the filename to be a GUID, got '{Path.GetFileNameWithoutExtension(savedPath)}'");
            Assert.Equal(ValidPngBytes, await File.ReadAllBytesAsync(savedPath));
        }

        [Fact]
        public async Task DownloadImageAsync_PrivateIpTarget_ThrowsWithoutMakingAnyHttpCall()
        {
            // Would still 200 if actually called - the point is asserting on
            // WasCalled below, not on what response it would have returned.
            var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(ValidPngBytes)
            });
            var service = CreateService(handler);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.DownloadImageAsync("http://127.0.0.1/test.png"));

            Assert.Contains("private", ex.Message, StringComparison.OrdinalIgnoreCase);
            Assert.False(handler.WasCalled, "The SSRF guard should have blocked this before any request was sent.");
        }

        [Fact]
        public async Task DownloadImageAsync_FileScheme_ThrowsWithoutMakingAnyHttpCall()
        {
            var handler = new FakeHttpMessageHandler(_ =>
                throw new InvalidOperationException("Only http/https should ever reach the HTTP client."));
            var service = CreateService(handler);

            await Assert.ThrowsAsync<ArgumentException>(() => service.DownloadImageAsync("file:///etc/passwd"));
        }

        [Fact]
        public async Task DownloadImageAsync_RedirectToPrivateIp_ThrowsAndStopsFollowing()
        {
            // 8.8.8.8 is a literal IP, so Dns.GetHostAddressesAsync resolves it
            // to itself with no real network call - lets the first host check
            // pass deterministically without depending on outbound DNS/internet
            // access in the test environment. The fake handler never actually
            // contacts 8.8.8.8 either; it's the transport itself.
            var handler = new FakeHttpMessageHandler(request =>
            {
                if (request.RequestUri!.Host == "8.8.8.8")
                {
                    var redirectResponse = new HttpResponseMessage(HttpStatusCode.Found);
                    redirectResponse.Headers.Location = new Uri("http://127.0.0.1/evil.png");
                    return redirectResponse;
                }

                throw new InvalidOperationException("The redirect target should have been blocked before being fetched.");
            });
            var service = CreateService(handler);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.DownloadImageAsync("http://8.8.8.8/start.png"));
            Assert.Contains("private", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task DownloadImageAsync_ResponseExceedsMaxSize_Throws()
        {
            var oversized = new byte[21 * 1024 * 1024]; // over the 20MB cap
            var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(oversized)
            });
            var service = CreateService(handler);

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.DownloadImageAsync("http://8.8.8.8/big.png"));
        }

        [Fact]
        public async Task DownloadImageAsync_ValidResponse_SavesWithRandomGuidFilename()
        {
            var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(ValidPngBytes)
            });
            var service = CreateService(handler);

            var savedPath = await service.DownloadImageAsync("http://8.8.8.8/photo.png");

            Assert.True(File.Exists(savedPath));
            Assert.Equal(".png", Path.GetExtension(savedPath));
            Assert.True(Guid.TryParseExact(Path.GetFileNameWithoutExtension(savedPath), "N", out _));
        }

        private sealed class FakeHttpMessageHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

            public bool WasCalled { get; private set; }

            public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
            {
                _responder = responder;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                WasCalled = true;
                return Task.FromResult(_responder(request));
            }
        }
    }
}
