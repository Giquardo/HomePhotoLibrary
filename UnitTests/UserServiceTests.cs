using Moq;
using PhotoAlbumApi.Models;
using PhotoAlbumApi.Repositories;
using PhotoAlbumApi.Services;

namespace PhotoAlbumApi.Tests
{
    public class UserServiceTests
    {
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly Mock<IAuthenticationService> _mockAuthService;
        private readonly UserService _service;

        public UserServiceTests()
        {
            _mockUserRepository = new Mock<IUserRepository>();
            _mockAuthService = new Mock<IAuthenticationService>();
            _service = new UserService(_mockUserRepository.Object, _mockAuthService.Object);
        }

        private static User MakeUser(string password, int accessFailedCount = 0, DateTime? lockoutEndUtc = null)
        {
            return new User
            {
                Id = 1,
                Username = "testuser",
                Email = "testuser@example.com",
                Password = BCrypt.Net.BCrypt.HashPassword(password),
                IsAdmin = false,
                AccessFailedCount = accessFailedCount,
                LockoutEndUtc = lockoutEndUtc
            };
        }

        [Fact]
        public async Task AuthenticateUserAsync_CorrectPassword_ReturnsUser()
        {
            var user = MakeUser("correctpassword");
            _mockUserRepository.Setup(r => r.GetUserByUsernameAsync("testuser")).ReturnsAsync(user);

            var result = await _service.AuthenticateUserAsync("testuser", "correctpassword");

            Assert.NotNull(result);
            Assert.Equal(user.Id, result.Id);
        }

        [Fact]
        public async Task AuthenticateUserAsync_WrongPassword_ReturnsNullAndIncrementsFailedCount()
        {
            var user = MakeUser("correctpassword");
            _mockUserRepository.Setup(r => r.GetUserByUsernameAsync("testuser")).ReturnsAsync(user);

            var result = await _service.AuthenticateUserAsync("testuser", "wrongpassword");

            Assert.Null(result);
            Assert.Equal(1, user.AccessFailedCount);
            _mockUserRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task AuthenticateUserAsync_UnknownUsername_ReturnsNullWithoutThrowing()
        {
            _mockUserRepository.Setup(r => r.GetUserByUsernameAsync("nosuchuser")).ReturnsAsync((User?)null);

            var result = await _service.AuthenticateUserAsync("nosuchuser", "anypassword");

            Assert.Null(result);
            _mockUserRepository.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task AuthenticateUserAsync_LockedOutUser_ReturnsNullWithoutVerifyingPassword()
        {
            var user = MakeUser("correctpassword", accessFailedCount: 0, lockoutEndUtc: DateTime.UtcNow.AddMinutes(10));
            _mockUserRepository.Setup(r => r.GetUserByUsernameAsync("testuser")).ReturnsAsync(user);

            // Even the correct password must be rejected while locked out.
            var result = await _service.AuthenticateUserAsync("testuser", "correctpassword");

            Assert.Null(result);
            _mockUserRepository.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task AuthenticateUserAsync_FifthFailedAttempt_SetsLockoutAndResetsCount()
        {
            var user = MakeUser("correctpassword", accessFailedCount: 4);
            _mockUserRepository.Setup(r => r.GetUserByUsernameAsync("testuser")).ReturnsAsync(user);

            var result = await _service.AuthenticateUserAsync("testuser", "wrongpassword");

            Assert.Null(result);
            Assert.Equal(0, user.AccessFailedCount);
            Assert.NotNull(user.LockoutEndUtc);
            Assert.True(user.LockoutEndUtc > DateTime.UtcNow);
        }

        [Fact]
        public async Task AuthenticateUserAsync_SuccessAfterPriorFailures_ResetsFailedCount()
        {
            var user = MakeUser("correctpassword", accessFailedCount: 3);
            _mockUserRepository.Setup(r => r.GetUserByUsernameAsync("testuser")).ReturnsAsync(user);

            var result = await _service.AuthenticateUserAsync("testuser", "correctpassword");

            Assert.NotNull(result);
            Assert.Equal(0, user.AccessFailedCount);
            Assert.Null(user.LockoutEndUtc);
        }

        [Fact]
        public async Task CreateUserAsync_HashesRawPasswordBeforeSaving()
        {
            User? savedUser = null;
            _mockUserRepository.Setup(r => r.CreateUserAsync(It.IsAny<User>()))
                .Callback<User>(u => savedUser = u)
                .ReturnsAsync((User u) => u);

            var newUser = new User { Username = "newuser", Email = "new@example.com", Password = "plaintextpassword123", IsAdmin = false };
            await _service.CreateUserAsync(newUser);

            Assert.NotNull(savedUser);
            Assert.NotEqual("plaintextpassword123", savedUser.Password);
            Assert.True(BCrypt.Net.BCrypt.Verify("plaintextpassword123", savedUser.Password));
        }

        [Fact]
        public async Task UpdateUserAsync_HashesRawPasswordBeforeSaving()
        {
            User? savedUser = null;
            _mockUserRepository.Setup(r => r.UpdateUserAsync(1, It.IsAny<User>()))
                .Callback<int, User>((id, u) => savedUser = u)
                .ReturnsAsync((int id, User u) => u);

            var updatedUser = new User { Username = "testuser", Email = "testuser@example.com", Password = "newplaintextpassword123", IsAdmin = false };
            await _service.UpdateUserAsync(1, updatedUser);

            Assert.NotNull(savedUser);
            Assert.NotEqual("newplaintextpassword123", savedUser.Password);
            Assert.True(BCrypt.Net.BCrypt.Verify("newplaintextpassword123", savedUser.Password));
        }
    }
}
