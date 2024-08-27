using Xunit;
using Moq;
using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using PhotoAlbumApi.Controllers;
using PhotoAlbumApi.Services;
using PhotoAlbumApi.DTOs;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using PhotoAlbumApi.Models;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

public class PhotoControllerTests
{
    private readonly Mock<IPhotoAlbumService> _mockService;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<ILoggingService> _mockLoggingService;
    private readonly Mock<IMemoryCache> _mockCache;
    private readonly PhotoController _controller;

    public PhotoControllerTests()
    {
        _mockService = new Mock<IPhotoAlbumService>();
        _mockMapper = new Mock<IMapper>();
        _mockLoggingService = new Mock<ILoggingService>();
        _mockCache = new Mock<IMemoryCache>();

        _controller = new PhotoController(_mockService.Object, _mockMapper.Object, _mockLoggingService.Object, _mockCache.Object);

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
    public async Task GetPhotosAsync_ReturnsOkObjectResult()
    {
        // Arrange
        var userId = 1;
        var photos = new List<Photo>
        {
            new Photo
            {
                Id = 1,
                AlbumId = 1,
                UserId = 1,
                Title = "Photo 1",
                Description = "Description",
                Url = "http://example.com/photo.jpg",
            }
        };
        var photoDtos = new List<PhotoDto>
        {
            new PhotoDto
            {
                AlbumId = 1,
                Title = "Photo 1",
                Description = "Description",
                Url = "http://example.com/photo.jpg",
            }
        };

        _mockService.Setup(s => s.GetPhotosAsync(userId)).ReturnsAsync(photos);
        _mockMapper.Setup(m => m.Map<IEnumerable<PhotoDto>>(photos)).Returns(photoDtos);

        object cacheValue;
        _mockCache.Setup(c => c.TryGetValue(It.IsAny<object>(), out cacheValue)).Returns(false);
        _mockCache.Setup(c => c.CreateEntry(It.IsAny<object>())).Returns(Mock.Of<ICacheEntry>());

        // Act
        var result = await _controller.GetPhotos();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
    }

    [Fact]
    public async Task GetPhotos_UnauthorizedAccess()
    {
        _mockService.Setup(s => s.GetPhotosAsync(It.IsAny<int>())).ThrowsAsync(new UnauthorizedAccessException());

        // Act
        var result = await _controller.GetPhotos();

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(StatusCodes.Status401Unauthorized, unauthorizedResult.StatusCode);
    }

    [Fact]
    public async Task GetPhotos_NotFound()
    {
        _mockService.Setup(s => s.GetPhotosAsync(It.IsAny<int>())).ReturnsAsync((IEnumerable<Photo>)null);

        // Act
        var result = await _controller.GetPhotos();

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
    }

    [Fact]
    public async Task GetPhoto_ReturnsOkObjectResult()
    {
        // Arrange
        var userId = 1;
        var photoId = 1;
        var photo = new Photo
        {
            Id = photoId,
            AlbumId = 1,
            UserId = userId,
            Title = "Photo 1",
            Description = "Description",
            Url = "http://example.com/photo.jpg",
        };
        var photoDto = new PhotoDisplayDto
        {
            AlbumId = 1,
            Title = "Photo 1",
            Description = "Description",
            Url = "http://example.com/photo.jpg",
        };

        _mockService.Setup(s => s.GetPhotoAsync(photoId, userId)).ReturnsAsync(photo);
        _mockMapper.Setup(m => m.Map<PhotoDisplayDto>(photo)).Returns(photoDto);

        object cacheValue;
        _mockCache.Setup(c => c.TryGetValue(It.IsAny<object>(), out cacheValue)).Returns(false);
        _mockCache.Setup(c => c.CreateEntry(It.IsAny<object>())).Returns(Mock.Of<ICacheEntry>());

        // Act
        var result = await _controller.GetPhoto(photoId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        Assert.Equal(photoDto, okResult.Value);
    }

    [Fact]
    public async Task GetPhoto_ReturnsNotFoundResult()
    {
        // Arrange
        var userId = 1;
        var photoId = 1;

        _mockService.Setup(s => s.GetPhotoAsync(photoId, userId)).ReturnsAsync((Photo)null);

        // Act
        var result = await _controller.GetPhoto(photoId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
    }

    [Fact]
    public async Task GetPhoto_ReturnsBadRequestResult()
    {
        // Arrange
        var userId = 1;
        var photoId = 1;
        var exceptionMessage = "Unauthorized access";

        _mockService.Setup(s => s.GetPhotoAsync(photoId, userId)).ThrowsAsync(new UnauthorizedAccessException(exceptionMessage));

        // Act
        var result = await _controller.GetPhoto(photoId);

        // Assert
        var unauthorizedRequestResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(StatusCodes.Status401Unauthorized, unauthorizedRequestResult.StatusCode);
        Assert.Equal(exceptionMessage, unauthorizedRequestResult.Value);
    }

    [Fact]
    public async Task UploadPhoto_ReturnsCreatedResult()
    {
        // Arrange
        var userId = 1;
        var photoUploadDto = new PhotoUploadDto
        {
            AlbumId = 1,
            Title = "New Photo",
            Description = "Description",
            File = Mock.Of<IFormFile>()
        };
        var photo = new Photo
        {
            AlbumId = photoUploadDto.AlbumId,
            Title = photoUploadDto.Title,
            Description = photoUploadDto.Description,
            UserId = userId
        };
        var addedPhoto = new Photo
        {
            Id = 1,
            AlbumId = photoUploadDto.AlbumId,
            Title = photoUploadDto.Title,
            Description = photoUploadDto.Description,
            UserId = userId
        };
        var photoDisplayDto = new PhotoDisplayDto
        {
            AlbumId = photoUploadDto.AlbumId,
            Title = photoUploadDto.Title,
            Description = photoUploadDto.Description,
            Url = "http://example.com/photo.jpg"
        };

        _mockService.Setup(s => s.AddPhotoAsync(It.IsAny<Photo>(), null, It.IsAny<IFormFile>())).ReturnsAsync(addedPhoto);
        _mockMapper.Setup(m => m.Map<PhotoDisplayDto>(addedPhoto)).Returns(photoDisplayDto);

        // Act
        var result = await _controller.UploadPhoto(photoUploadDto);

        // Assert
        var createdResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status201Created, createdResult.StatusCode);
        Assert.Equal(photoDisplayDto, createdResult.Value);
    }

    [Fact]
    public async Task UploadPhoto_ReturnsConflictResult()
    {
        // Arrange
        var photoUploadDto = new PhotoUploadDto
        {
            AlbumId = 1,
            Title = "New Photo",
            Description = "Description",
            File = Mock.Of<IFormFile>()
        };
        var exceptionMessage = "Photo already exists";

        _mockService.Setup(s => s.AddPhotoAsync(It.IsAny<Photo>(), null, It.IsAny<IFormFile>())).ThrowsAsync(new InvalidOperationException(exceptionMessage));

        // Act
        var result = await _controller.UploadPhoto(photoUploadDto);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(StatusCodes.Status409Conflict, conflictResult.StatusCode);
    }

    [Fact]
    public async Task UploadPhoto_returnsUnauthorizedAccess()
    {
        // Arrange
        var photoUploadDto = new PhotoUploadDto
        {
            AlbumId = 1,
            Title = "New Photo",
            Description = "Description",
            File = Mock.Of<IFormFile>()
        };
        var exceptionMessage = "Unauthorized access";

        _mockService.Setup(s => s.AddPhotoAsync(It.IsAny<Photo>(), null, It.IsAny<IFormFile>())).ThrowsAsync(new UnauthorizedAccessException(exceptionMessage));

        // Act
        var result = await _controller.UploadPhoto(photoUploadDto);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(StatusCodes.Status401Unauthorized, unauthorizedResult.StatusCode);
    }

    [Fact]
    public async Task UploadPhoto_ReturnsInternalServerError()
    {
        // Arrange
        var photoUploadDto = new PhotoUploadDto
        {
            AlbumId = 1,
            Title = "New Photo",
            Description = "Description",
            File = Mock.Of<IFormFile>()
        };
        var exceptionMessage = "An unexpected error occurred. Please try again later.";

        _mockService.Setup(s => s.AddPhotoAsync(It.IsAny<Photo>(), null, It.IsAny<IFormFile>())).ThrowsAsync(new Exception(exceptionMessage));

        // Act
        var result = await _controller.UploadPhoto(photoUploadDto);

        // Assert
        var internalServerErrorResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, internalServerErrorResult.StatusCode);
        Assert.Equal(exceptionMessage, internalServerErrorResult.Value);
    }

    [Fact]
    public async Task AddPhoto_ReturnsCreatedResult()
    {
        // Arrange
        var photoDto = new PhotoDto { AlbumId = 1, Title = "New Photo", Description = "Description", Url = "http://example.com/photo.jpg" };
        var userId = 1;
        var photo = new Photo { Id = 1, AlbumId = 1, Title = "New Photo", Description = "Description", UserId = userId, Url = "http://example.com/photo.jpg" };
        var photoDisplayDto = new PhotoDisplayDto { Id = 1, Title = "New Photo", Description = "Description", Url = "http://example.com/photo.jpg" };

        _mockService.Setup(s => s.AddPhotoAsync(It.IsAny<Photo>(), It.IsAny<string>(), null)).ReturnsAsync(photo);
        _mockMapper.Setup(m => m.Map<PhotoDisplayDto>(It.IsAny<Photo>())).Returns(photoDisplayDto);

        // Act
        var result = await _controller.AddPhoto(photoDto);

        // Assert
        var createdResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status201Created, createdResult.StatusCode);
        Assert.Equal(photoDisplayDto, createdResult.Value);
    }

    [Fact]
    public async Task AddPhoto_ReturnsConflictResult()
    {
        // Arrange
        var photoDto = new PhotoDto { AlbumId = 1, Title = "New Photo", Description = "Description", Url = "http://example.com/photo.jpg" };
        var exceptionMessage = "Conflict occurred";

        _mockService.Setup(s => s.AddPhotoAsync(It.IsAny<Photo>(), It.IsAny<string>(), null)).ThrowsAsync(new InvalidOperationException(exceptionMessage));

        // Act
        var result = await _controller.AddPhoto(photoDto);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(StatusCodes.Status409Conflict, conflictResult.StatusCode);
    }

    [Fact]
    public async Task AddPhoto_ReturnsUnauthorizedResult()
    {
        // Arrange
        var photoDto = new PhotoDto { AlbumId = 1, Title = "New Photo", Description = "Description", Url = "http://example.com/photo.jpg" };
        var exceptionMessage = "Unauthorized access";

        _mockService.Setup(s => s.AddPhotoAsync(It.IsAny<Photo>(), It.IsAny<string>(), null)).ThrowsAsync(new UnauthorizedAccessException(exceptionMessage));

        // Act
        var result = await _controller.AddPhoto(photoDto);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(StatusCodes.Status401Unauthorized, unauthorizedResult.StatusCode);
        Assert.Equal(exceptionMessage, unauthorizedResult.Value);
    }

    [Fact]
    public async Task AddPhoto_ReturnsInternalServerError()
    {
        // Arrange
        var photoDto = new PhotoDto { AlbumId = 1, Title = "New Photo", Description = "Description", Url = "http://example.com/photo.jpg" };
        var exceptionMessage = "An unexpected error occurred. Please try again later.";

        _mockService.Setup(s => s.AddPhotoAsync(It.IsAny<Photo>(), It.IsAny<string>(), null)).ThrowsAsync(new Exception(exceptionMessage));

        // Act
        var result = await _controller.AddPhoto(photoDto);

        // Assert
        var internalServerErrorResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, internalServerErrorResult.StatusCode);
        Assert.Equal(exceptionMessage, internalServerErrorResult.Value);
    }

    [Fact]
    public async Task UpdatePhoto_ReturnsOkResult()
    {
        // Arrange
        var photoUpdateDto = new PhotoUpdateDto { AlbumId = 1, Title = "Updated Photo", Description = "Updated Description" };
        var userId = 1;
        var photoId = 1;
        var existingPhoto = new Photo { Id = photoId, AlbumId = 1, Title = "Old Photo", Description = "Old Description", UserId = userId };
        var updatedPhoto = new Photo { Id = photoId, AlbumId = 1, Title = "Updated Photo", Description = "Updated Description", UserId = userId };
        var photoDisplayDto = new PhotoDisplayDto { Id = photoId, Title = "Updated Photo", Description = "Updated Description", Url = "http://example.com/photo.jpg" };

        _mockService.Setup(s => s.GetPhotoAsync(photoId, userId)).ReturnsAsync(existingPhoto);
        _mockService.Setup(s => s.UpdatePhotoAsync(It.IsAny<Photo>())).ReturnsAsync(updatedPhoto);
        _mockMapper.Setup(m => m.Map<PhotoDisplayDto>(It.IsAny<Photo>())).Returns(photoDisplayDto);

        // Act
        var result = await _controller.UpdatePhoto(photoId, photoUpdateDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        Assert.Equal(photoDisplayDto, okResult.Value);
    }

    [Fact]
    public async Task UpdatePhoto_ReturnsBadRequest_WhenPhotoUpdateDtoIsNull()
    {
        // Arrange
        PhotoUpdateDto photoUpdateDto = null;

        // Act
        var result = await _controller.UpdatePhoto(1, photoUpdateDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
    }

    [Fact]
    public async Task UpdatePhoto_ReturnsNotFound_WhenPhotoDoesNotExist()
    {
        // Arrange
        var photoUpdateDto = new PhotoUpdateDto { AlbumId = 1, Title = "Updated Photo", Description = "Updated Description" };
        var userId = 1;
        var photoId = 1;

        _mockService.Setup(s => s.GetPhotoAsync(photoId, userId)).ReturnsAsync((Photo)null);

        // Act
        var result = await _controller.UpdatePhoto(photoId, photoUpdateDto);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
    }

    [Fact]
    public async Task UpdatePhoto_ReturnsInternalServerError_WhenUpdateFails()
    {
        // Arrange
        var photoUpdateDto = new PhotoUpdateDto { AlbumId = 1, Title = "Updated Photo", Description = "Updated Description" };
        var userId = 1;
        var photoId = 1;
        var existingPhoto = new Photo { Id = photoId, AlbumId = 1, Title = "Old Photo", Description = "Old Description", UserId = userId };

        _mockService.Setup(s => s.GetPhotoAsync(photoId, userId)).ReturnsAsync(existingPhoto);
        _mockService.Setup(s => s.UpdatePhotoAsync(It.IsAny<Photo>())).ReturnsAsync((Photo)null);

        // Act
        var result = await _controller.UpdatePhoto(photoId, photoUpdateDto);

        // Assert
        var internalServerErrorResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, internalServerErrorResult.StatusCode);
        Assert.Equal(new { message = "An error occurred while updating the photo" }.ToString(), internalServerErrorResult.Value.ToString());
    }

    [Fact]
    public async Task UpdatePhoto_ReturnsBadRequest_WhenUnauthorizedAccessExceptionIsThrown()
    {
        // Arrange
        var photoUpdateDto = new PhotoUpdateDto { AlbumId = 1, Title = "Updated Photo", Description = "Updated Description" };
        var userId = 1;
        var photoId = 1;
        var exceptionMessage = "Unauthorized access";

        _mockService.Setup(s => s.GetPhotoAsync(photoId, userId)).ThrowsAsync(new UnauthorizedAccessException(exceptionMessage));

        // Act
        var result = await _controller.UpdatePhoto(photoId, photoUpdateDto);

        // Assert
        var badRequestResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
        Assert.Equal(exceptionMessage, badRequestResult.Value);
    }

    [Fact]
    public async Task DownloadPhoto_ReturnsFileResult_WhenPhotoExists()
    {
        // Arrange
        var userId = 1;
        var photoId = 1;
        var photoFileDto = new PhotoFileDto
        {
            FileData = new byte[] { 1, 2, 3 },
            ContentType = "image/jpeg",
            FileName = "photo.jpg"
        };

        _mockService.Setup(s => s.GetPhotoFileAsync(photoId, userId)).ReturnsAsync(photoFileDto);

        // Act
        var result = await _controller.DownloadPhoto(photoId);

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal(photoFileDto.FileData, fileResult.FileContents);
        Assert.Equal(photoFileDto.ContentType, fileResult.ContentType);
        Assert.Equal(photoFileDto.FileName, fileResult.FileDownloadName);
    }

    [Fact]
    public async Task DownloadPhoto_ReturnsNotFound_WhenPhotoDoesNotExist()
    {
        // Arrange
        var userId = 1;
        var photoId = 1;

        _mockService.Setup(s => s.GetPhotoFileAsync(photoId, userId)).ReturnsAsync((PhotoFileDto)null);

        // Act
        var result = await _controller.DownloadPhoto(photoId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
        Assert.Equal(new { message = "Photo not found", photoId = photoId }.ToString(), notFoundResult.Value.ToString());
    }

    [Fact]
    public async Task DownloadPhoto_ReturnsBadRequest_WhenUnauthorizedAccessExceptionIsThrown()
    {
        // Arrange
        var userId = 1;
        var photoId = 1;
        var exceptionMessage = "Unauthorized access";

        _mockService.Setup(s => s.GetPhotoFileAsync(photoId, userId)).ThrowsAsync(new UnauthorizedAccessException(exceptionMessage));

        // Act
        var result = await _controller.DownloadPhoto(photoId);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
        Assert.Equal(exceptionMessage, badRequestResult.Value);
    }

    [Fact]
    public async Task DeletePhoto_ReturnsOkResult_WhenPhotoIsDeleted()
    {
        // Arrange
        var userId = 1;
        var photoId = 1;
        var existingPhoto = new Photo { Id = photoId, UserId = userId };

        _mockService.Setup(s => s.GetPhotoAsync(photoId, userId)).ReturnsAsync(existingPhoto);
        _mockService.Setup(s => s.DeletePhotoAsync(photoId, userId)).Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeletePhoto(photoId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        Assert.Equal("Photo deleted successfully", okResult.Value);
    }

    [Fact]
    public async Task DeletePhoto_ReturnsNotFound_WhenPhotoDoesNotExist()
    {
        // Arrange
        var userId = 1;
        var photoId = 1;

        _mockService.Setup(s => s.GetPhotoAsync(photoId, userId)).ReturnsAsync((Photo)null);

        // Act
        var result = await _controller.DeletePhoto(photoId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
        Assert.Equal(new { message = "Photo not found", photoId = photoId }.ToString(), notFoundResult.Value.ToString());
    }

    [Fact]
    public async Task DeletePhoto_ReturnsBadRequest_WhenUnauthorizedAccessExceptionIsThrown()
    {
        // Arrange
        var userId = 1;
        var photoId = 1;
        var exceptionMessage = "Unauthorized access";

        _mockService.Setup(s => s.GetPhotoAsync(photoId, userId)).ThrowsAsync(new UnauthorizedAccessException(exceptionMessage));

        // Act
        var result = await _controller.DeletePhoto(photoId);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
        Assert.Equal(exceptionMessage, badRequestResult.Value);
    }

    [Fact]
    public async Task UndoDeletePhoto_ReturnsOkResult_WhenPhotoIsRestored()
    {
        // Arrange
        var userId = 1;
        var photoId = 1;
        var photo = new Photo { Id = photoId, UserId = userId };
        var photoDisplayDto = new PhotoDisplayDto { Id = photoId };

        _mockService.Setup(s => s.UndoDeletePhotoAsync(photoId, userId)).ReturnsAsync(photo);
        _mockMapper.Setup(m => m.Map<PhotoDisplayDto>(photo)).Returns(photoDisplayDto);

        // Act
        var result = await _controller.UndoDeletePhoto(photoId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        Assert.Equal(photoDisplayDto, okResult.Value);
    }

    [Fact]
    public async Task UndoDeletePhoto_ReturnsNotFound_WhenPhotoNotFoundOrNotDeleted()
    {
        // Arrange
        var userId = 1;
        var photoId = 1;

        _mockService.Setup(s => s.UndoDeletePhotoAsync(photoId, userId)).ReturnsAsync((Photo)null);

        // Act
        var result = await _controller.UndoDeletePhoto(photoId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
        Assert.Equal(new { message = "Photo not found or not deleted", photoId = photoId }.ToString(), notFoundResult.Value.ToString());
    }

    [Fact]
    public async Task UndoDeletePhoto_ReturnsUnauthorized_WhenUnauthorizedAccessExceptionIsThrown()
    {
        // Arrange
        var userId = 1;
        var photoId = 1;
        var exceptionMessage = "Unauthorized access";

        _mockService.Setup(s => s.UndoDeletePhotoAsync(photoId, userId)).ThrowsAsync(new UnauthorizedAccessException(exceptionMessage));

        // Act
        var result = await _controller.UndoDeletePhoto(photoId);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(StatusCodes.Status401Unauthorized, unauthorizedResult.StatusCode);
        Assert.Equal(exceptionMessage, unauthorizedResult.Value);
    }
}
