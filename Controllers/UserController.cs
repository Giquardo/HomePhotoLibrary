using Microsoft.AspNetCore.Mvc;
using PhotoAlbumApi.Models;
using PhotoAlbumApi.Services;
using AutoMapper;
using PhotoAlbumApi.DTOs;
using Microsoft.Extensions.Caching.Memory;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace PhotoAlbumApi.Controllers;

[ApiController]
[Route("api/users")]
public class UserController : ControllerBase
{
    private readonly IUserService _service;
    private readonly IAuthenticationService _authService;
    private readonly LoggingService _loggingService;
    private readonly IMemoryCache _cache;
    private readonly IMapper _mapper;

    public UserController(IUserService service, IAuthenticationService authService, LoggingService loggingService, IMemoryCache cache, IMapper mapper)
    {
        _service = service;
        _authService = authService;
        _loggingService = loggingService;
        _cache = cache;
        _mapper = mapper;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginModel loginModel)
    {
        _loggingService.LogInformation($"Logging in user: {loginModel.Username}");
        var user = await _service.AuthenticateUserAsync(loginModel.Username, loginModel.Password);
        if (user == null)
        {
            return StatusCode(StatusCodes.Status401Unauthorized, new { message = "Invalid username or password" });
        }
        _loggingService.LogInformation($"Successfully logged in user: {loginModel.Username}");
        var token = _authService.GenerateJwtToken(user);
        return StatusCode(StatusCodes.Status200OK, new { token });
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllUsers()
    {
        _loggingService.LogInformation("Fetching all users");

        if (!_cache.TryGetValue("AllUsers", out IEnumerable<User> users))
        {
            users = await _service.GetUsersAsync();
            if (users == null)
            {
                _loggingService.LogWarning("No users found");
                return StatusCode(StatusCodes.Status404NotFound);
            }
            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(5));
            _cache.Set("AllUsers", users, cacheEntryOptions);
        }

        var userDisplayDtos = _mapper.Map<IEnumerable<UserDisplayDto>>(users);
        return StatusCode(StatusCodes.Status200OK, userDisplayDtos);
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetUser(int id)
    {
        _loggingService.LogInformation($"Fetching user with ID: {id}");

        if (!_cache.TryGetValue($"User_{id}", out User user))
        {
            user = await _service.GetUserByIdAsync(id);
            if (user == null)
            {
                _loggingService.LogWarning($"User with ID: {id} not found");
                return StatusCode(StatusCodes.Status404NotFound);
            }
            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(5));
            _cache.Set($"User_{id}", user, cacheEntryOptions);
        }

        _loggingService.LogInformation($"Successfully fetched user with ID: {id}");
        var userDisplayDto = _mapper.Map<UserDisplayDto>(user);
        return StatusCode(StatusCodes.Status200OK, userDisplayDto);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateUser([FromBody] UserDto userDto)
    {
        _loggingService.LogInformation($"Creating user: {userDto.Username}");
        var user = _mapper.Map<User>(userDto);
        var createdUser = await _service.CreateUserAsync(user);
        var userDisplayDto = _mapper.Map<UserDisplayDto>(createdUser);
        return StatusCode(StatusCodes.Status201Created, new { id = createdUser.Id, userDisplayDto });
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UserDto userDto)
    {
        _loggingService.LogInformation($"Updating user with ID: {id}");
        var user = _mapper.Map<User>(userDto);
        var updatedUser = await _service.UpdateUserAsync(id, user);
        if (updatedUser == null)
        {
            _loggingService.LogWarning($"User with ID: {id} not found");
            return StatusCode(StatusCodes.Status404NotFound);
        }
        _cache.Remove($"User_{id}");
        var userDisplayDto = _mapper.Map<UserDisplayDto>(updatedUser);
        return StatusCode(StatusCodes.Status200OK, userDisplayDto);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        _loggingService.LogInformation($"Deleting user with ID: {id}");
        await _service.DeleteUserAsync(id);
        _cache.Remove($"User_{id}");
        return StatusCode(StatusCodes.Status204NoContent);
    }
}
