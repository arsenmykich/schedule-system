using Microsoft.EntityFrameworkCore;
using Moq;
using ScheduleSystem.Data;
using ScheduleSystem.Models;
using ScheduleSystem.Services;
using Xunit;

namespace ScheduleSystem.Tests;

/// <summary>
/// Comprehensive unit tests for the meeting scheduling algorithm
/// </summary>
public class MeetingSchedulingServiceTests : IDisposable
{
    private readonly ScheduleDbContext _context;
    private readonly Mock<IUserService> _mockUserService;
    private readonly MeetingSchedulingService _service;

    public MeetingSchedulingServiceTests()
    {
        var options = new DbContextOptionsBuilder<ScheduleDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ScheduleDbContext(options);
        _mockUserService = new Mock<IUserService>();
        _service = new MeetingSchedulingService(_context, _mockUserService.Object);

        // Setup test data
        SetupTestData();
    }

    private void SetupTestData()
    {
        var users = new[]
        {
            new User { Id = 1, Name = "Alice" },
            new User { Id = 2, Name = "Bob" },
            new User { Id = 3, Name = "Charlie" }
        };

        _context.Users.AddRange(users);
        _context.SaveChanges();

        _mockUserService.Setup(x => x.UsersExistAsync(It.IsAny<IEnumerable<int>>()))
                       .ReturnsAsync(true);
    }

    [Fact]
    public async Task ScheduleMeetingAsync_WithNoConflicts_ShouldScheduleAtEarliestTime()
    {
        // Arrange
        var request = new CreateMeetingRequest
        {
            ParticipantIds = new List<int> { 1, 2 },
            DurationMinutes = 60,
            EarliestStart = DateTime.UtcNow.Date.AddHours(9),  // 9:00 AM
            LatestEnd = DateTime.UtcNow.Date.AddHours(17)      // 5:00 PM
        };

        // Act
        var result = await _service.ScheduleMeetingAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.EarliestStart, result.StartTime);
        Assert.Equal(request.EarliestStart.AddMinutes(60), result.EndTime);
        Assert.Equal(request.ParticipantIds, result.ParticipantIds);
    }

    [Fact]
    public async Task ScheduleMeetingAsync_WithExistingMeeting_ShouldFindNextAvailableSlot()
    {
        // Arrange
        var existingMeeting = new Meeting
        {
            Id = 1,
            StartTime = DateTime.UtcNow.Date.AddHours(9),   // 9:00 AM
            EndTime = DateTime.UtcNow.Date.AddHours(10),    // 10:00 AM
            ParticipantIds = new List<int> { 1 },
            Participants = new List<User> { _context.Users.First(u => u.Id == 1) }
        };

        _context.Meetings.Add(existingMeeting);
        await _context.SaveChangesAsync();

        var request = new CreateMeetingRequest
        {
            ParticipantIds = new List<int> { 1, 2 },
            DurationMinutes = 60,
            EarliestStart = DateTime.UtcNow.Date.AddHours(9),  // 9:00 AM
            LatestEnd = DateTime.UtcNow.Date.AddHours(17)      // 5:00 PM
        };

        // Act
        var result = await _service.ScheduleMeetingAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(DateTime.UtcNow.Date.AddHours(10), result.StartTime); // 10:00 AM
        Assert.Equal(DateTime.UtcNow.Date.AddHours(11), result.EndTime);   // 11:00 AM
    }

    [Fact]
    public async Task ScheduleMeetingAsync_WithOverlappingMeetings_ShouldMergeAndFindGap()
    {
        // Arrange
        var meetings = new[]
        {
            new Meeting
            {
                StartTime = DateTime.UtcNow.Date.AddHours(9),   // 9:00 AM
                EndTime = DateTime.UtcNow.Date.AddHours(10),    // 10:00 AM
                ParticipantIds = new List<int> { 1 },
                Participants = new List<User> { _context.Users.First(u => u.Id == 1) }
            },
            new Meeting
            {
                StartTime = DateTime.UtcNow.Date.AddHours(9.5), // 9:30 AM (overlapping)
                EndTime = DateTime.UtcNow.Date.AddHours(11),     // 11:00 AM
                ParticipantIds = new List<int> { 1 },
                Participants = new List<User> { _context.Users.First(u => u.Id == 1) }
            }
        };

        _context.Meetings.AddRange(meetings);
        await _context.SaveChangesAsync();

        var request = new CreateMeetingRequest
        {
            ParticipantIds = new List<int> { 1 },
            DurationMinutes = 60,
            EarliestStart = DateTime.UtcNow.Date.AddHours(9),
            LatestEnd = DateTime.UtcNow.Date.AddHours(17)
        };

        // Act
        var result = await _service.ScheduleMeetingAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(DateTime.UtcNow.Date.AddHours(11), result.StartTime); // 11:00 AM (after merged slot)
        Assert.Equal(DateTime.UtcNow.Date.AddHours(12), result.EndTime);   // 12:00 PM
    }

    [Fact]
    public async Task ScheduleMeetingAsync_WithBackToBackMeetings_ShouldScheduleAfter()
    {
        // Arrange
        var existingMeeting = new Meeting
        {
            StartTime = DateTime.UtcNow.Date.AddHours(9),   // 9:00 AM
            EndTime = DateTime.UtcNow.Date.AddHours(10),    // 10:00 AM
            ParticipantIds = new List<int> { 1 },
            Participants = new List<User> { _context.Users.First(u => u.Id == 1) }
        };

        _context.Meetings.Add(existingMeeting);
        await _context.SaveChangesAsync();

        var request = new CreateMeetingRequest
        {
            ParticipantIds = new List<int> { 1 },
            DurationMinutes = 30,
            EarliestStart = DateTime.UtcNow.Date.AddHours(9),
            LatestEnd = DateTime.UtcNow.Date.AddHours(17)
        };

        // Act
        var result = await _service.ScheduleMeetingAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(DateTime.UtcNow.Date.AddHours(10), result.StartTime); // 10:00 AM (right after existing)
        Assert.Equal(DateTime.UtcNow.Date.AddHours(10.5), result.EndTime); // 10:30 AM
    }

    [Fact]
    public async Task ScheduleMeetingAsync_WithNoAvailableSlot_ShouldReturnNull()
    {
        // Arrange - fill entire day with meetings
        var allDayMeeting = new Meeting
        {
            StartTime = DateTime.UtcNow.Date.AddHours(9),   // 9:00 AM
            EndTime = DateTime.UtcNow.Date.AddHours(17),    // 5:00 PM
            ParticipantIds = new List<int> { 1 },
            Participants = new List<User> { _context.Users.First(u => u.Id == 1) }
        };

        _context.Meetings.Add(allDayMeeting);
        await _context.SaveChangesAsync();

        var request = new CreateMeetingRequest
        {
            ParticipantIds = new List<int> { 1 },
            DurationMinutes = 60,
            EarliestStart = DateTime.UtcNow.Date.AddHours(9),
            LatestEnd = DateTime.UtcNow.Date.AddHours(17)
        };

        // Act
        var result = await _service.ScheduleMeetingAsync(request);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ScheduleMeetingAsync_WithBusinessHoursConstraint_ShouldRespectHours()
    {
        // Arrange
        var request = new CreateMeetingRequest
        {
            ParticipantIds = new List<int> { 1 },
            DurationMinutes = 60,
            EarliestStart = DateTime.UtcNow.Date.AddHours(8),   // 8:00 AM (before business hours)
            LatestEnd = DateTime.UtcNow.Date.AddHours(18)       // 6:00 PM (after business hours)
        };

        // Act
        var result = await _service.ScheduleMeetingAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(DateTime.UtcNow.Date.AddHours(9), result.StartTime);  // Should start at 9:00 AM (business hours)
        Assert.Equal(DateTime.UtcNow.Date.AddHours(10), result.EndTime);   // Should end at 10:00 AM
    }

    [Fact]
    public async Task ScheduleMeetingAsync_WithMeetingTooLongForBusinessHours_ShouldReturnNull()
    {
        // Arrange
        var request = new CreateMeetingRequest
        {
            ParticipantIds = new List<int> { 1 },
            DurationMinutes = 600, // 10 hours (longer than business day)
            EarliestStart = DateTime.UtcNow.Date.AddHours(9),
            LatestEnd = DateTime.UtcNow.Date.AddHours(17)
        };

        // Act
        var result = await _service.ScheduleMeetingAsync(request);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ScheduleMeetingAsync_WithInvalidParticipants_ShouldThrowException()
    {
        // Arrange
        _mockUserService.Setup(x => x.UsersExistAsync(It.IsAny<IEnumerable<int>>()))
                       .ReturnsAsync(false);

        var request = new CreateMeetingRequest
        {
            ParticipantIds = new List<int> { 999 }, // Non-existent user
            DurationMinutes = 60,
            EarliestStart = DateTime.UtcNow.Date.AddHours(9),
            LatestEnd = DateTime.UtcNow.Date.AddHours(17)
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.ScheduleMeetingAsync(request));
    }

    [Fact]
    public async Task ScheduleMeetingAsync_WithInvalidTimeWindow_ShouldThrowException()
    {
        // Arrange
        var request = new CreateMeetingRequest
        {
            ParticipantIds = new List<int> { 1 },
            DurationMinutes = 60,
            EarliestStart = DateTime.UtcNow.Date.AddHours(17), // 5:00 PM
            LatestEnd = DateTime.UtcNow.Date.AddHours(9)       // 9:00 AM (invalid: end before start)
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.ScheduleMeetingAsync(request));
    }

    [Fact]
    public async Task ScheduleMeetingAsync_WithPartialOverlaps_ShouldHandleCorrectly()
    {
        // Arrange
        var existingMeetings = new[]
        {
            new Meeting
            {
                StartTime = DateTime.UtcNow.Date.AddHours(9),    // 9:00 AM
                EndTime = DateTime.UtcNow.Date.AddHours(9.5),    // 9:30 AM
                ParticipantIds = new List<int> { 1 },
                Participants = new List<User> { _context.Users.First(u => u.Id == 1) }
            },
            new Meeting
            {
                StartTime = DateTime.UtcNow.Date.AddHours(10.5), // 10:30 AM
                EndTime = DateTime.UtcNow.Date.AddHours(11),     // 11:00 AM
                ParticipantIds = new List<int> { 1 },
                Participants = new List<User> { _context.Users.First(u => u.Id == 1) }
            }
        };

        _context.Meetings.AddRange(existingMeetings);
        await _context.SaveChangesAsync();

        var request = new CreateMeetingRequest
        {
            ParticipantIds = new List<int> { 1 },
            DurationMinutes = 60, // 1 hour - won't fit in 30-minute gap
            EarliestStart = DateTime.UtcNow.Date.AddHours(9),
            LatestEnd = DateTime.UtcNow.Date.AddHours(17)
        };

        // Act
        var result = await _service.ScheduleMeetingAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(DateTime.UtcNow.Date.AddHours(9.5), result.StartTime); // In the gap between meetings (9:30 AM)
        Assert.Equal(DateTime.UtcNow.Date.AddHours(10.5), result.EndTime); // End at 10:30 AM
    }

    [Fact]
    public async Task ScheduleMeetingAsync_WithMultipleParticipants_ShouldConsiderAllSchedules()
    {
        // Arrange
        var aliceMeeting = new Meeting
        {
            StartTime = DateTime.UtcNow.Date.AddHours(9),   // 9:00 AM
            EndTime = DateTime.UtcNow.Date.AddHours(10),    // 10:00 AM
            ParticipantIds = new List<int> { 1 },
            Participants = new List<User> { _context.Users.First(u => u.Id == 1) }
        };

        var bobMeeting = new Meeting
        {
            StartTime = DateTime.UtcNow.Date.AddHours(10),  // 10:00 AM
            EndTime = DateTime.UtcNow.Date.AddHours(11),    // 11:00 AM
            ParticipantIds = new List<int> { 2 },
            Participants = new List<User> { _context.Users.First(u => u.Id == 2) }
        };

        _context.Meetings.AddRange(new[] { aliceMeeting, bobMeeting });
        await _context.SaveChangesAsync();

        var request = new CreateMeetingRequest
        {
            ParticipantIds = new List<int> { 1, 2 }, // Both Alice and Bob
            DurationMinutes = 60,
            EarliestStart = DateTime.UtcNow.Date.AddHours(9),
            LatestEnd = DateTime.UtcNow.Date.AddHours(17)
        };

        // Act
        var result = await _service.ScheduleMeetingAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(DateTime.UtcNow.Date.AddHours(11), result.StartTime); // After both meetings
        Assert.Equal(DateTime.UtcNow.Date.AddHours(12), result.EndTime);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}