using Microsoft.AspNetCore.Mvc;
using PhotoAlbumApi.Models;
using PhotoAlbumApi.Services;
using AutoMapper;
using PhotoAlbumApi.DTOs;
using Microsoft.Extensions.Caching.Memory;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace PhotoAlbumApi.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _service;
        private readonly IAuthenticationService _authService;
        private readonly LoggingService _loggingService;
        private readonly IMemoryCache _cache;

        public UserController(IUserService service, IAuthenticationService authService, LoggingService loggingService, IMemoryCache cache)
        {
            _service = service;
            _authService = authService;
            _loggingService = loggingService;
            _cache = cache;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel loginModel)
        {
            _loggingService.LogInformation($"Logging in user: {loginModel.Username}");
            var user = await _service.AuthenticateUserAsync(loginModel.Username, loginModel.Password);
            if (user == null)
            {
                return Unauthorized(new { message = "Invalid username or password" });
            }
            _loggingService.LogInformation($"Successfully logged in user: {loginModel.Username}");
            var token = _authService.GenerateJwtToken(user);
            return Ok(new { token });
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllUsers()
        {
            _loggingService.LogInformation("Fetching all users");
            var users = await _service.GetUsersAsync();
            return Ok(users);
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetUser(int id)
        {
            _loggingService.LogInformation($"Fetching user with ID: {id}");
            var user = await _service.GetUserByIdAsync(id);
            if (user == null)
            {
                _loggingService.LogWarning($"User with ID: {id} not found");
                return NotFound();
            }
            _loggingService.LogInformation($"Successfully fetched user with ID: {id}");
            return Ok(user);
        }
    }
}