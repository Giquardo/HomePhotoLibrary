using Moq;
using Microsoft.AspNetCore.Mvc;
using PhotoAlbumApi.Controllers;
using PhotoAlbumApi.Models;
using PhotoAlbumApi.Services;
using PhotoAlbumApi.DTOs;
using Microsoft.Extensions.Caching.Memory;
using AutoMapper;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Http;

namespace PhotoAlbumApi.Tests
{
    public class UserControllerTests
    {
        private readonly Mock<IUserService> _mockUserService;
        private readonly Mock<IAuthenticationService> _mockAuthService;
        private readonly Mock<ILoggingService> _mockLoggingService;
        private readonly Mock<IMemoryCache> _mockCache;
        private readonly Mock<IMapper> _mockMapper;
        private readonly UserController _controller;

        public UserControllerTests()
        {
            _mockUserService = new Mock<IUserService>();
            _mockAuthService = new Mock<IAuthenticationService>();
            _mockLoggingService = new Mock<ILoggingService>();
            _mockCache = new Mock<IMemoryCache>();
            _mockMapper = new Mock<IMapper>();

            _controller = new UserController(_mockUserService.Object, _mockAuthService.Object, _mockLoggingService.Object, _mockCache.Object, _mockMapper.Object);
        }

        [Fact]
        public async Task Login_CorrectUsernameAndPassword_ReturnsOkWithToken()
        {
            // Arrange
            var loginModel = new LoginModel
            {
                Username = "testuser",
                Password = "correctpassword"
            };

            var user = new User
            {
                Id = 1,
                Username = loginModel.Username,
                Email = "testuser@example.com",
                Password = "hashedpassword",
                IsAdmin = false
            };

            var token = "generated-jwt-token";

            _mockUserService.Setup(s => s.AuthenticateUserAsync(loginModel.Username, loginModel.Password))
                .ReturnsAsync(user);
            _mockAuthService.Setup(a => a.GenerateJwtToken(user))
                .Returns(token);

            // Act
            var result = await _controller.Login(loginModel) as ObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);

            var jsonObject = JObject.FromObject(result.Value);
            Assert.Equal(token, jsonObject["token"].ToString());

            // Verify that methods were called as expected
            _mockUserService.Verify(s => s.AuthenticateUserAsync(loginModel.Username, loginModel.Password), Times.Once);
            _mockAuthService.Verify(a => a.GenerateJwtToken(user), Times.Once);
            _mockLoggingService.Verify(l => l.LogInformation(It.IsAny<string>()), Times.AtLeast(2));
        }

        [Fact]
        public async Task Login_IncorrectUsernameOrPassword_ReturnsUnauthorized()
        {
            // Arrange
            var loginModel = new LoginModel
            {
                Username = "testuser",
                Password = "wrongpassword"
            };

            // Mock the IUserService to return null for incorrect credentials
            _mockUserService.Setup(s => s.AuthenticateUserAsync(loginModel.Username, loginModel.Password))
                .ReturnsAsync((User)null);

            // Act
            var result = await _controller.Login(loginModel) as ObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(401, result.StatusCode);

            var jsonObject = JObject.FromObject(result.Value);
            Assert.Equal("Invalid username or password", jsonObject["message"]?.ToString());

            // Verify that AuthenticateUserAsync was called once with the incorrect credentials
            _mockUserService.Verify(s => s.AuthenticateUserAsync(loginModel.Username, loginModel.Password), Times.Once);
            _mockLoggingService.Verify(l => l.LogInformation(It.IsAny<string>()), Times.AtLeast(1)); // logging the failed login attempt
        }

        [Fact]
        public async Task GetUser_ValidId_ReturnsUser()
        {
            // Arrange
            var userId = 1;
            var user = new User
            {
                Id = userId,
                Username = "testuser",
                Email = "testuser@example.com",
                Password = "hashedpassword",
                IsAdmin = false
            };

            var userDisplayDto = new UserDisplayDto
            {
                Id = userId,
                Username = "testuser",
                Email = "testuser@example.com"
            };

            _mockUserService.Setup(s => s.GetUserByIdAsync(userId)).ReturnsAsync(user);
            _mockMapper.Setup(m => m.Map<UserDisplayDto>(user)).Returns(userDisplayDto);

            // Setup the cache mock
            _mockCache.Setup(c => c.TryGetValue(It.IsAny<object>(), out It.Ref<object>.IsAny)).Returns(false);
            var mockCacheEntry = new Mock<ICacheEntry>();
            _mockCache.Setup(c => c.CreateEntry(It.IsAny<object>())).Returns(mockCacheEntry.Object);

            // Act
            var result = await _controller.GetUser(userId) as ObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            Assert.IsType<UserDisplayDto>(result.Value);
            Assert.Equal(userDisplayDto, result.Value);

            // Verify that methods were called as expected
            _mockUserService.Verify(s => s.GetUserByIdAsync(userId), Times.Once);
            _mockMapper.Verify(m => m.Map<UserDisplayDto>(user), Times.Once);
            _mockLoggingService.Verify(l => l.LogInformation(It.IsAny<string>()), Times.AtLeast(2));
            _mockCache.Verify(c => c.TryGetValue(It.IsAny<object>(), out It.Ref<object>.IsAny), Times.Once);
            _mockCache.Verify(c => c.CreateEntry(It.IsAny<object>()), Times.Once);
        }

        [Fact]
        public async Task GetUser_InvalidId_ReturnsNotFound()
        {
            // Arrange
            var userId = 1;

            _mockUserService.Setup(s => s.GetUserByIdAsync(userId)).ReturnsAsync((User)null);

            // Setup the cache mock
            _mockCache.Setup(c => c.TryGetValue(It.IsAny<object>(), out It.Ref<object>.IsAny)).Returns(false);

            // Act
            var result = await _controller.GetUser(userId) as StatusCodeResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(404, result.StatusCode);

            // Verify that methods were called as expected
            _mockUserService.Verify(s => s.GetUserByIdAsync(userId), Times.Once);
            _mockLoggingService.Verify(l => l.LogWarning(It.IsAny<string>()), Times.Once);
            _mockCache.Verify(c => c.TryGetValue(It.IsAny<object>(), out It.Ref<object>.IsAny), Times.Once);
        }

        [Fact]
        public async Task GetAllUsers_ReturnsUsers()
        {
            // Arrange
            var users = new List<User>
    {
        new User
        {
            Id = 1,
            Username = "testuser1",
            Email = "testuser1@example.com",
            Password = "hashedpassword1",
            IsAdmin = false
        },
        new User
        {
            Id = 2,
            Username = "testuser2",
            Email = "testuser2@example.com",
            Password = "hashedpassword2",
            IsAdmin = false
        }
    };

            var userDisplayDtos = new List<UserDisplayDto>
    {
        new UserDisplayDto
        {
            Id = 1,
            Username = "testuser1",
            Email = "testuser1@example.com"
        },
        new UserDisplayDto
        {
            Id = 2,
            Username = "testuser2",
            Email = "testuser2@example.com"
        }
    };

            _mockUserService.Setup(s => s.GetUsersAsync()).ReturnsAsync(users);
            _mockMapper.Setup(m => m.Map<IEnumerable<UserDisplayDto>>(users)).Returns(userDisplayDtos);

            // Setup the cache mock
            _mockCache.Setup(c => c.TryGetValue(It.IsAny<object>(), out It.Ref<object>.IsAny)).Returns(false);
            var mockCacheEntry = new Mock<ICacheEntry>();
            _mockCache.Setup(c => c.CreateEntry(It.IsAny<object>())).Returns(mockCacheEntry.Object);

            // Act
            var result = await _controller.GetAllUsers() as ObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            Assert.IsType<List<UserDisplayDto>>(result.Value);
            Assert.Equal(userDisplayDtos, result.Value);

            // Verify that methods were called as expected
            _mockUserService.Verify(s => s.GetUsersAsync(), Times.Once);
            _mockMapper.Verify(m => m.Map<IEnumerable<UserDisplayDto>>(users), Times.Once);
            _mockLoggingService.Verify(l => l.LogInformation(It.IsAny<string>()), Times.Once);
            _mockCache.Verify(c => c.TryGetValue(It.IsAny<object>(), out It.Ref<object>.IsAny), Times.Once);
            _mockCache.Verify(c => c.CreateEntry(It.IsAny<object>()), Times.Once);
        }

        [Fact]
        public async Task GetAllUsers_NoUsersFound_ReturnsNotFound()
        {
            // Arrange
            IEnumerable<User> users = null;
            _mockUserService.Setup(s => s.GetUsersAsync()).ReturnsAsync(users);

            // Setup the cache mock
            _mockCache.Setup(c => c.TryGetValue(It.IsAny<object>(), out It.Ref<object>.IsAny)).Returns(false);

            // Act
            var result = await _controller.GetAllUsers() as StatusCodeResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(404, result.StatusCode);

            // Verify that methods were called as expected
            _mockUserService.Verify(s => s.GetUsersAsync(), Times.Once);
            _mockLoggingService.Verify(l => l.LogWarning(It.IsAny<string>()), Times.Once);
            _mockCache.Verify(c => c.TryGetValue(It.IsAny<object>(), out It.Ref<object>.IsAny), Times.Once);
        }

        [Fact]
        public async Task CreateUser_ReturnsBadRequest_WhenModelStateIsInvalid()
        {
            // Arrange
            _controller.ModelState.AddModelError("Username", "Required");

            // Act
            var result = await _controller.CreateUser(new UserDto());

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Invalid user data", badRequestResult.Value);
        }

        [Fact]
        public async Task CreateUser_Returns201Created_WhenModelStateIsValid()
        {
            // Arrange
            var userDto = new UserDto
            {
                Username = "testuser",
                Email = "testuser@example.com",
                Password = "password123",
                IsAdmin = true
            };
            var user = new User
            {
                Id = 1,
                Username = "testuser",
                Email = "testuser@example.com",
                Password = "password123",
                IsAdmin = true,
                Albums = new List<Album>()
            };
            var userDisplayDto = new UserDisplayDto
            {
                Id = 1,
                Username = "testuser",
                Email = "testuser@example.com",
                IsAdmin = true,
                Albums = new List<AlbumDto>()
            };

            _mockMapper.Setup(m => m.Map<User>(userDto)).Returns(user);
            _mockUserService.Setup(s => s.CreateUserAsync(user)).ReturnsAsync(user);
            _mockMapper.Setup(m => m.Map<UserDisplayDto>(user)).Returns(userDisplayDto);

            // Act
            var result = await _controller.CreateUser(userDto);

            // Assert
            var createdResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status201Created, createdResult.StatusCode);

            var resultValue = Assert.IsType<UserDisplayDto>(createdResult.Value);
            Assert.NotNull(resultValue);

            // Additional assertions to verify the returned object
            Assert.Equal(user.Id, resultValue.Id);
            Assert.Equal(userDisplayDto.Username, resultValue.Username);
            Assert.Equal(userDisplayDto.Email, resultValue.Email);
            Assert.Equal(userDisplayDto.IsAdmin, resultValue.IsAdmin);
            Assert.Equal(userDisplayDto.Albums, resultValue.Albums);
        }

        [Fact]
        public async Task UpdateUser_Returns404NotFound_WhenUserIdIsInvalid()
        {
            // Arrange
            var invalidUserId = 999;
            var userDto = new UserDto
            {
                Username = "testuser",
                Email = "testuser@example.com",
                Password = "password123",
                IsAdmin = true
            };
            var user = new User
            {
                Id = invalidUserId,
                Username = "testuser",
                Email = "testuser@example.com",
                Password = "password123",
                IsAdmin = true,
                Albums = new List<Album>()
            };

            _mockMapper.Setup(m => m.Map<User>(userDto)).Returns(user);
            _mockUserService.Setup(s => s.UpdateUserAsync(invalidUserId, user)).ReturnsAsync((User)null);

            // Act
            var result = await _controller.UpdateUser(invalidUserId, userDto);

            // Assert
            var notFoundResult = Assert.IsType<StatusCodeResult>(result);
            Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
        }

        [Fact]
        public async Task UpdateUser_Returns400BadRequest_WhenUserDtoIsInvalid()
        {
            // Arrange
            var validUserId = 1;
            var invalidUserDto = new UserDto
            {
                Username = "", // Invalid username
                Email = "invalid-email", // Invalid email
                Password = "", // Invalid password
                IsAdmin = true
            };

            _controller.ModelState.AddModelError("Username", "Required");
            _controller.ModelState.AddModelError("Email", "Invalid format");
            _controller.ModelState.AddModelError("Password", "Required");

            // Act
            var result = await _controller.UpdateUser(validUserId, invalidUserDto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
        }

        [Fact]
        public async Task UpdateUser_Returns200Ok_WhenUserDtoIsValidAndUserIdIsFound()
        {
            // Arrange
            var validUserId = 1;
            var userDto = new UserDto
            {
                Username = "testuser",
                Email = "testuser@example.com",
                Password = "password123",
                IsAdmin = true
            };
            var user = new User
            {
                Id = validUserId,
                Username = "testuser",
                Email = "testuser@example.com",
                Password = "password123",
                IsAdmin = true,
                Albums = new List<Album>()
            };
            var updatedUser = new User
            {
                Id = validUserId,
                Username = "updateduser",
                Email = "updateduser@example.com",
                Password = "newpassword123",
                IsAdmin = false,
                Albums = new List<Album>()
            };
            var albumDtos = new List<AlbumDto>(); // Assuming you have a list of AlbumDto
            var userDisplayDto = new UserDisplayDto
            {
                Id = validUserId,
                Username = "updateduser",
                Email = "updateduser@example.com",
                IsAdmin = false,
                Albums = albumDtos
            };

            _mockMapper.Setup(m => m.Map<User>(userDto)).Returns(user);
            _mockUserService.Setup(s => s.UpdateUserAsync(validUserId, user)).ReturnsAsync(updatedUser);
            _mockMapper.Setup(m => m.Map<ICollection<AlbumDto>>(updatedUser.Albums)).Returns(albumDtos);
            _mockMapper.Setup(m => m.Map<UserDisplayDto>(updatedUser)).Returns(userDisplayDto);

            // Act
            var result = await _controller.UpdateUser(validUserId, userDto);

            // Assert
            var okResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            var resultValue = Assert.IsType<UserDisplayDto>(okResult.Value);
            Assert.NotNull(resultValue);
            Assert.Equal(userDisplayDto.Id, resultValue.Id);
            Assert.Equal(userDisplayDto.Username, resultValue.Username);
            Assert.Equal(userDisplayDto.Email, resultValue.Email);
            Assert.Equal(userDisplayDto.IsAdmin, resultValue.IsAdmin);
            Assert.Equal(userDisplayDto.Albums, resultValue.Albums);
        }

        [Fact]
        public async Task DeleteUser_Returns204NoContent_WhenUserIdIsValid()
        {
            // Arrange
            var validUserId = 1;
            var user = new User
            {
                Id = validUserId,
                Username = "testuser",
                Email = "testuser@example.com",
                Password = "password123",
                IsAdmin = true,
                Albums = new List<Album>()
            };

            _mockUserService.Setup(s => s.GetUserByIdAsync(validUserId)).ReturnsAsync(user);
            _mockUserService.Setup(s => s.DeleteUserAsync(validUserId)).Returns(Task.CompletedTask);
            _mockCache.Setup(c => c.Remove(It.IsAny<string>()));

            // Act
            var result = await _controller.DeleteUser(validUserId);

            // Assert
            var noContentResult = Assert.IsType<StatusCodeResult>(result);
            Assert.Equal(StatusCodes.Status204NoContent, noContentResult.StatusCode);
            _mockUserService.Verify(s => s.DeleteUserAsync(validUserId), Times.Once);
            _mockCache.Verify(c => c.Remove($"User_{validUserId}"), Times.Once);
        }

        [Fact]
        public async Task DeleteUser_Returns404NotFound_WhenUserIdIsNotFound()
        {
            // Arrange
            var invalidUserId = 999;

            _mockUserService.Setup(s => s.GetUserByIdAsync(invalidUserId)).ReturnsAsync((User)null);

            // Act
            var result = await _controller.DeleteUser(invalidUserId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundResult>(result);
            Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
            _mockUserService.Verify(s => s.DeleteUserAsync(It.IsAny<int>()), Times.Never);
            _mockCache.Verify(c => c.Remove(It.IsAny<string>()), Times.Never);
        }
    }
}