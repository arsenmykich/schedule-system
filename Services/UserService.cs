using Microsoft.EntityFrameworkCore;
using ScheduleSystem.Data;
using ScheduleSystem.Models;

namespace ScheduleSystem.Services;

/// <summary>
/// Service for managing user operations
/// </summary>
public class UserService : IUserService
{
    private readonly ScheduleDbContext _context;

    public UserService(ScheduleDbContext context)
    {
        _context = context;
    }

    public async Task<User> CreateUserAsync(CreateUserRequest request)
    {
        var user = new User
        {
            Name = request.Name
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task<User?> GetUserByIdAsync(int id)
    {
        return await _context.Users
            .Include(u => u.Meetings)
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<IEnumerable<User>> GetAllUsersAsync()
    {
        return await _context.Users.ToListAsync();
    }

    public async Task<bool> UsersExistAsync(IEnumerable<int> userIds)
    {
        var existingUserIds = await _context.Users
            .Where(u => userIds.Contains(u.Id))
            .Select(u => u.Id)
            .ToListAsync();

        return userIds.All(id => existingUserIds.Contains(id));
    }

    public async Task<IEnumerable<Meeting>> GetUserMeetingsAsync(int userId)
    {
        var user = await _context.Users
            .Include(u => u.Meetings)
            .ThenInclude(m => m.Participants)
            .FirstOrDefaultAsync(u => u.Id == userId);

        return user?.Meetings ?? new List<Meeting>();
    }
}