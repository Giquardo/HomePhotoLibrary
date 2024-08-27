using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using PhotoAlbumApi.Controllers;
using PhotoAlbumApi.Models;
using PhotoAlbumApi.Services;
using PhotoAlbumApi.DTOs;
using Microsoft.Extensions.Caching.Memory;
using AutoMapper;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;



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



    }
}
