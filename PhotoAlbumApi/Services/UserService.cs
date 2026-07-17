using PhotoAlbumApi.Models;
using PhotoAlbumApi.Repositories;

namespace PhotoAlbumApi.Services;
public interface IUserService
{
    Task<User?> AuthenticateUserAsync(string username, string password);
    Task<IEnumerable<User>?> GetUsersAsync();
    Task<User?> GetUserByIdAsync(int id);
    Task<User> CreateUserAsync(User user);
    Task<User?> UpdateUserAsync(int id, User user);
    Task DeleteUserAsync(int id);
}

public class UserService : IUserService
{
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    // Verified against on every "user not found" path so that lookup timing doesn't
    // reveal whether a username exists.
    private static readonly string DummyPasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString());

    private readonly IUserRepository _userRepository;
    private readonly IAuthenticationService _authenticationService;

    public UserService(IUserRepository userRepository, IAuthenticationService authenticationService)
    {
        _userRepository = userRepository;
        _authenticationService = authenticationService;
    }

    public async Task<User?> AuthenticateUserAsync(string username, string password)
    {
        var user = await _userRepository.GetUserByUsernameAsync(username);

        if (user == null)
        {
            BCrypt.Net.BCrypt.Verify(password, DummyPasswordHash);
            return null;
        }

        if (user.LockoutEndUtc.HasValue && user.LockoutEndUtc.Value > DateTime.UtcNow)
        {
            return null;
        }

        if (!BCrypt.Net.BCrypt.Verify(password, user.Password))
        {
            user.AccessFailedCount++;
            if (user.AccessFailedCount >= MaxFailedAttempts)
            {
                user.LockoutEndUtc = DateTime.UtcNow.Add(LockoutDuration);
                user.AccessFailedCount = 0;
            }
            await _userRepository.SaveChangesAsync();
            return null;
        }

        if (user.AccessFailedCount != 0 || user.LockoutEndUtc.HasValue)
        {
            user.AccessFailedCount = 0;
            user.LockoutEndUtc = null;
            await _userRepository.SaveChangesAsync();
        }

        return user;
    }

    public async Task<IEnumerable<User>?> GetUsersAsync()
    {
        return await _userRepository.GetUsersAsync();
    }

    public async Task<User?> GetUserByIdAsync(int id)
    {
        return await _userRepository.GetUserByIdAsync(id);
    }

    public async Task<User> CreateUserAsync(User user)
    {
        user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);
        return await _userRepository.CreateUserAsync(user);
    }

    public async Task<User?> UpdateUserAsync(int id, User user)
    {
        // Empty/null means "keep the existing password" - only hash and
        // overwrite when a new one was actually supplied.
        if (!string.IsNullOrEmpty(user.Password))
        {
            user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);
        }
        return await _userRepository.UpdateUserAsync(id, user);
    }

    public async Task DeleteUserAsync(int id)
    {
        await _userRepository.DeleteUserAsync(id);
    }
}
