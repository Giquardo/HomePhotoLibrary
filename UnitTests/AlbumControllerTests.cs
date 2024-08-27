using Xunit;
using Moq;
using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using PhotoAlbumApi.Controllers;
using PhotoAlbumApi.Services;
using PhotoAlbumApi.Models;
using PhotoAlbumApi.DTOs;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

public class AlbumControllerTests
{
    private readonly Mock<IPhotoAlbumService> _mockService;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<ILoggingService> _mockLoggingService;
    private readonly Mock<IMemoryCache> _mockCache;
    private readonly AlbumController _controller;

    public AlbumControllerTests()
    {
        _mockService = new Mock<IPhotoAlbumService>();
        _mockMapper = new Mock<IMapper>();
        _mockLoggingService = new Mock<ILoggingService>();
        _mockCache = new Mock<IMemoryCache>();
        _controller = new AlbumController(_mockService.Object, _mockMapper.Object, _mockLoggingService.Object, _mockCache.Object);

        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim("UserId", "1")
        }, "mock"));

        _controller.ControllerContext = new ControllerContext()
        {
            HttpContext = new DefaultHttpContext() { User = user }
        };
    }

    [Fact]
    public async Task GetAlbumsV1_Returns200OK_WhenAlbumsExist()
    {
        // Arrange
        var userId = 1;
        var albums = new List<Album> { new Album { Id = 1, Title = "Test Album" } };
        var albumDtos = new List<AlbumSummaryDto> { new AlbumSummaryDto { Id = 1, Title = "Test Album" } };

        _mockService.Setup(s => s.GetAlbumsAsync(userId)).ReturnsAsync(albums);
        _mockMapper.Setup(m => m.Map<IEnumerable<AlbumSummaryDto>>(albums)).Returns(albumDtos);

        object cacheValue;
        _mockCache.Setup(c => c.TryGetValue(It.IsAny<object>(), out cacheValue)).Returns(false);
        _mockCache.Setup(c => c.CreateEntry(It.IsAny<object>())).Returns(Mock.Of<ICacheEntry>());

        // Act
        var result = await _controller.GetAlbumsV1();

        // Assert
        var okResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
    }

    [Fact]
    public async Task GetAlbumsV1_Returns400BadRequest_WhenUnauthorizedAccessExceptionIsThrown()
    {
        // Arrange
        _mockService.Setup(s => s.GetAlbumsAsync(It.IsAny<int>())).ThrowsAsync(new UnauthorizedAccessException());

        // Act
        var result = await _controller.GetAlbumsV1();

        // Assert
        var badRequestResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
    }

    [Fact]
    public async Task GetAlbumsV1_Returns200OK_WithEmptyList_WhenNoAlbumsExist()
    {
        // Arrange
        var userId = 1;
        var albums = new List<Album>(); // No albums
        var albumDtos = new List<AlbumSummaryDto>(); // Corresponding empty DTO list

        _mockService.Setup(s => s.GetAlbumsAsync(userId)).ReturnsAsync(albums);
        _mockMapper.Setup(m => m.Map<IEnumerable<AlbumSummaryDto>>(albums)).Returns(albumDtos);

        object cacheValue;
        _mockCache.Setup(c => c.TryGetValue(It.IsAny<object>(), out cacheValue)).Returns(false);
        _mockCache.Setup(c => c.CreateEntry(It.IsAny<object>())).Returns(Mock.Of<ICacheEntry>());

        // Act
        var result = await _controller.GetAlbumsV1();

        // Assert
        var okResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
    }

    [Fact]
    public async Task GetAlbumV1_AlbumFound_ReturnsOk()
    {
        // Arrange
        var userId = 1;
        var albumId = 1;
        var album = new Album();
        var albumSummaryDto = new AlbumSummaryDto();
        var cacheKey = $"GetAlbumV1_{userId}_{albumId}";

        _mockService.Setup(x => x.GetAlbumAsync(albumId, userId)).ReturnsAsync(album);
        _mockMapper.Setup(x => x.Map<AlbumSummaryDto>(album)).Returns(albumSummaryDto);

        // Mock cache entry
        var mockCacheEntry = new Mock<ICacheEntry>();
        mockCacheEntry.SetupAllProperties();
        _mockCache.Setup(c => c.CreateEntry(It.IsAny<object>())).Returns(mockCacheEntry.Object);

        // Act
        var result = await _controller.GetAlbumV1(albumId);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, objectResult.StatusCode);
    }

    [Fact]
    public async Task GetAlbumV1_AlbumNotFound_ReturnsNotFound()
    {
        // Arrange
        var userId = 1;
        var albumId = 1;
        var cacheKey = $"GetAlbumV1_{userId}_{albumId}";

        _mockService.Setup(x => x.GetAlbumAsync(albumId, userId)).ReturnsAsync((Album)null);

        // Mock cache entry
        var mockCacheEntry = new Mock<ICacheEntry>();
        mockCacheEntry.SetupAllProperties();
        _mockCache.Setup(c => c.CreateEntry(It.IsAny<object>())).Returns(mockCacheEntry.Object);

        // Act
        var result = await _controller.GetAlbumV1(albumId);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, objectResult.StatusCode);

    }

    [Fact]
    public async Task GetAlbumV1_UnauthorizedAccess_ReturnsBadRequest()
    {
        // Arrange
        var userId = 1;
        var albumId = 1;
        var cacheKey = $"GetAlbumV1_{userId}_{albumId}";

        _mockService.Setup(x => x.GetAlbumAsync(albumId, userId)).ThrowsAsync(new UnauthorizedAccessException("Unauthorized access"));

        // Mock cache entry
        var mockCacheEntry = new Mock<ICacheEntry>();
        mockCacheEntry.SetupAllProperties();
        _mockCache.Setup(c => c.CreateEntry(It.IsAny<object>())).Returns(mockCacheEntry.Object);

        // Act
        var result = await _controller.GetAlbumV1(albumId);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, objectResult.StatusCode);
    }

    [Fact]
    public async Task AddAlbum_ValidAlbumDto_ReturnsCreatedResult()
    {
        // Arrange
        var albumDto = new AlbumDto { Title = "Test Album", Description = "Test Description" };
        var album = new Album { Id = 1, Title = "Test Album", Description = "Test Description", UserId = 1 };
        var albumSummaryDto = new AlbumSummaryDto { Id = 1, Title = "Test Album", Description = "Test Description", UserId = 1 };

        _mockMapper.Setup(m => m.Map<Album>(albumDto)).Returns(album);
        _mockService.Setup(s => s.AddAlbumAsync(album)).ReturnsAsync(album);
        _mockMapper.Setup(m => m.Map<AlbumSummaryDto>(album)).Returns(albumSummaryDto);

        // Act
        var result = await _controller.AddAlbum(albumDto);

        // Assert
        var createdResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status201Created, createdResult.StatusCode);
    }

    [Fact]
    public async Task AddAlbum_UnauthorizedAccessException_ReturnsUnauthorized()
    {
        // Arrange
        var albumDto = new AlbumDto { Title = "Test Album", Description = "Test Description" };
        var exceptionMessage = "Unauthorized access";

        _mockMapper.Setup(m => m.Map<Album>(albumDto)).Throws(new UnauthorizedAccessException(exceptionMessage));

        // Act
        var result = await _controller.AddAlbum(albumDto);

        // Assert
        var unauthorizedResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status401Unauthorized, unauthorizedResult.StatusCode);
    }

    [Fact]
    public async Task AddAlbum_InvalidAlbumData_ReturnsBadRequest()
    {
        // Arrange
        var albumDto = new AlbumDto { Title = "", Description = "Test Description" }; // Invalid data (empty title)
        _controller.ModelState.AddModelError("Title", "The Title field is required.");

        // Act
        var result = await _controller.AddAlbum(albumDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
    }

    [Fact]
    public async Task UpdateAlbum_UnauthorizedAccessException_ReturnsUnauthorized()
    {
        // Arrange
        var albumDto = new AlbumDto { Title = "New Title", Description = "New Description" };
        var exceptionMessage = "Unauthorized access";

        // Mock the service to throw UnauthorizedAccessException
        _mockService.Setup(s => s.GetAlbumAsync(It.IsAny<int>(), It.IsAny<int>()))
                    .ThrowsAsync(new UnauthorizedAccessException(exceptionMessage));

        // Act
        var result = await _controller.UpdateAlbum(1, albumDto);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(StatusCodes.Status401Unauthorized, unauthorizedResult.StatusCode);
    }

    [Fact]
    public async Task UpdateAlbum_InvalidAlbumData_ReturnsBadRequest()
    {
        // Arrange
        AlbumDto invalidAlbumDto = null; // Simulating invalid album data by setting it to null

        // Act
        var result = await _controller.UpdateAlbum(1, invalidAlbumDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
    }

    [Fact]
    public async Task UpdateAlbum_AlbumNotFound_ReturnsNotFound()
    {
        // Arrange
        var albumId = 1;
        var userId = 1;
        var validAlbumDto = new AlbumDto { Title = "Test Album", Description = "Test Description" };

        // Mock the service to return null for the album
        _mockService.Setup(s => s.GetAlbumAsync(albumId, userId)).ReturnsAsync((Album)null);

        // Act
        var result = await _controller.UpdateAlbum(albumId, validAlbumDto);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
    }

    [Fact]
    public async Task UpdateAlbum_InternalServerError_ReturnsInternalServerError()
    {
        // Arrange
        var albumId = 1;
        var userId = 1;
        var validAlbumDto = new AlbumDto { Title = "Test Album", Description = "Test Description" };

        // Mock the service to return a valid album for the initial get
        _mockService.Setup(s => s.GetAlbumAsync(albumId, userId)).ReturnsAsync(new Album { Id = albumId, UserId = userId });

        // Mock the service to return null for the update, simulating an internal server error
        _mockService.Setup(s => s.UpdateAlbumAsync(It.IsAny<Album>())).ReturnsAsync((Album)null);

        // Act
        var result = await _controller.UpdateAlbum(albumId, validAlbumDto);

        // Assert
        var internalServerErrorResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, internalServerErrorResult.StatusCode);
    }


    [Fact]
    public async Task UpdateAlbum_Success_ReturnsOk()
    {
        // Arrange
        var albumId = 1;
        var userId = 1;
        var validAlbumDto = new AlbumDto { Title = "Test Album", Description = "Test Description" };
        var updatedAlbum = new Album { Id = albumId, Title = "Test Album", Description = "Test Description", UserId = userId };

        // Mock the service to return a valid album for the initial get
        _mockService.Setup(s => s.GetAlbumAsync(albumId, userId)).ReturnsAsync(new Album { Id = albumId, UserId = userId });

        // Mock the service to return the updated album
        _mockService.Setup(s => s.UpdateAlbumAsync(It.IsAny<Album>())).ReturnsAsync(updatedAlbum);

        // Mock the mapper to return the album summary DTO
        var albumSummaryDto = new AlbumSummaryDto { Id = albumId, Title = "Test Album", Description = "Test Description" };
        _mockMapper.Setup(m => m.Map<AlbumSummaryDto>(updatedAlbum)).Returns(albumSummaryDto);

        // Act
        var result = await _controller.UpdateAlbum(albumId, validAlbumDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
    }

    [Fact]
    public async Task DeleteAlbum_Unauthorized_ReturnsBadRequest()
    {
        // Arrange
        var albumId = 1;
        var userId = 1;
        _mockService.Setup(s => s.GetAlbumAsync(albumId, userId)).ThrowsAsync(new UnauthorizedAccessException("Unauthorized access"));

        // Act
        var result = await _controller.DeleteAlbum(albumId);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
    }

    [Fact]
    public async Task DeleteAlbum_AlbumNotFound_ReturnsNotFound()
    {
        // Arrange
        var albumId = 1;
        var userId = 1;
        _mockService.Setup(s => s.GetAlbumAsync(albumId, userId)).ReturnsAsync((Album)null);

        // Act
        var result = await _controller.DeleteAlbum(albumId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
    }

    [Fact]
    public async Task DeleteAlbum_Success_ReturnsOk()
    {
        // Arrange
        var albumId = 1;
        var userId = 1;
        var album = new Album { Id = albumId, UserId = userId };
        _mockService.Setup(s => s.GetAlbumAsync(albumId, userId)).ReturnsAsync(album);
        _mockService.Setup(s => s.DeleteAlbumAsync(albumId, userId)).Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeleteAlbum(albumId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
    }

    [Fact]
    public async Task UndoDeleteAlbum_Success_ReturnsOk()
    {
        // Arrange
        var albumId = 1;
        var userId = 1;
        var album = new Album { Id = albumId, UserId = userId };
        var albumSummaryDto = new AlbumSummaryDto { Id = albumId, UserId = userId };
        _mockService.Setup(s => s.UndoDeleteAlbumAsync(albumId, userId)).ReturnsAsync(album);
        _mockMapper.Setup(m => m.Map<AlbumSummaryDto>(album)).Returns(albumSummaryDto);

        // Act
        var result = await _controller.UndoDeleteAlbum(albumId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        var response = Assert.IsType<AlbumSummaryDto>(okResult.Value);
        Assert.Equal(albumSummaryDto, response);
    }

    [Fact]
    public async Task UndoDeleteAlbum_AlbumNotFound_ReturnsNotFound()
    {
        // Arrange
        var albumId = 1;
        var userId = 1;
        _mockService.Setup(s => s.UndoDeleteAlbumAsync(albumId, userId)).ReturnsAsync((Album)null);

        // Act
        var result = await _controller.UndoDeleteAlbum(albumId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);

    }

    [Fact]
    public async Task UndoDeleteAlbum_UnauthorizedAccess_ReturnsBadRequest()
    {
        // Arrange
        var albumId = 1;
        var userId = 1;
        var exceptionMessage = "Unauthorized access";
        _mockService.Setup(s => s.UndoDeleteAlbumAsync(albumId, userId)).ThrowsAsync(new UnauthorizedAccessException(exceptionMessage));

        // Act
        var result = await _controller.UndoDeleteAlbum(albumId);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
    }

}