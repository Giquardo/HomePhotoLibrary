using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using PhotoAlbumApi;
using PhotoAlbumApi.DTOs;
using PhotoAlbumApi.Models;
using Newtonsoft.Json;
using AutoMapper;
using System.Net;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using PhotoAlbumApi.Services;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Net.Http.Headers;

namespace PhotoAlbumApi.IntegrationTests
{
    public class UserControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;
        private readonly Mock<IUserService> _userServiceMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILoggingService> _loggingServiceMock;
        private readonly Mock<IMemoryCache> _cacheMock;
        private readonly Mock<IAuthenticationService> _authServiceMock;

        public UserControllerIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _userServiceMock = new Mock<IUserService>();
            _mapperMock = new Mock<IMapper>();
            _loggingServiceMock = new Mock<ILoggingService>();
            _cacheMock = new Mock<IMemoryCache>();
            _authServiceMock = new Mock<IAuthenticationService>();

            var customFactory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll(typeof(IUserService));
                    services.RemoveAll(typeof(IMapper));
                    services.RemoveAll(typeof(ILoggingService));
                    services.RemoveAll(typeof(IMemoryCache));
                    services.RemoveAll(typeof(IAuthenticationService));

                    services.AddSingleton(_userServiceMock.Object);
                    services.AddSingleton(_mapperMock.Object);
                    services.AddSingleton(_loggingServiceMock.Object);
                    services.AddSingleton(_cacheMock.Object);
                    services.AddSingleton(_authServiceMock.Object);

                });
            });

            _client = customFactory.CreateClient();
        }

        [Fact]
        public async Task Login_ReturnsUnauthorized_WhenInvalidCredentials()
        {
            // Arrange
            var loginModel = new LoginModel { Username = "invalidUser", Password = "invalidPass" };
            _userServiceMock.Setup(s => s.AuthenticateUserAsync(loginModel.Username, loginModel.Password))
                .ReturnsAsync((User?)null);

            // Act
            var response = await _client.PostAsJsonAsync("/api/users/login", loginModel);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Login_ReturnsOk_WhenValidCredentials()
        {
            // Arrange
            var loginModel = new LoginModel { Username = "validUser", Password = "validPass" };
            var user = new User { Username = "validUser" }; // Mock user object
            var token = "mockJwtToken";

            _userServiceMock.Setup(s => s.AuthenticateUserAsync(loginModel.Username, loginModel.Password))
                .ReturnsAsync(user);
            _authServiceMock.Setup(s => s.GenerateJwtToken(user))
                .Returns(token);

            // Act
            var response = await _client.PostAsJsonAsync("/api/users/login", loginModel);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var responseContent = await response.Content.ReadAsStringAsync();
            Assert.Contains(token, responseContent);
        }


        // [Fact]
        // public async Task GetAllUsers_ReturnsNotFound_WhenNoUsersAreFound()
        // {
        //     // Arrange
        //     _userServiceMock.Setup(s => s.GetUsersAsync()).ReturnsAsync((IEnumerable<User>?)null);

        //     // Act
        //     var response = await _client.GetAsync("/api/users");

        //     // Assert
        //     Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        // }

        // [Fact]
        // public async Task GetAllUsers_ReturnsUnauthorized_WhenUserIsNotAuthorized()
        // {
        //     // Arrange
        //     _client.DefaultRequestHeaders.Authorization = null; // Remove authorization header

        //     // Act
        //     var response = await _client.GetAsync("/api/users");

        //     // Assert
        //     Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        // }
    }
}