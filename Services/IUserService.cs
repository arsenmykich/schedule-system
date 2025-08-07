using ScheduleSystem.Models;

namespace ScheduleSystem.Services;

/// <summary>
/// Interface for user management operations
/// </summary>
public interface IUserService
{
    Task<User> CreateUserAsync(CreateUserRequest request);
    Task<User?> GetUserByIdAsync(int id);
    Task<IEnumerable<User>> GetAllUsersAsync();
    Task<bool> UsersExistAsync(IEnumerable<int> userIds);
    Task<IEnumerable<Meeting>> GetUserMeetingsAsync(int userId);
}