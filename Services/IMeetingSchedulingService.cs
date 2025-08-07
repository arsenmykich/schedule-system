using ScheduleSystem.Models;

namespace ScheduleSystem.Services;

/// <summary>
/// Interface for meeting scheduling operations
/// </summary>
public interface IMeetingSchedulingService
{
    Task<MeetingResponse?> ScheduleMeetingAsync(CreateMeetingRequest request);
    Task<IEnumerable<MeetingResponse>> GetAllMeetingsAsync();
    Task<MeetingResponse?> GetMeetingByIdAsync(int id);
}

/// <summary>
/// Represents a time slot for scheduling
/// </summary>
public class TimeSlot
{
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    
    public bool OverlapsWith(TimeSlot other)
    {
        return Start < other.End && End > other.Start;
    }
    
    public bool OverlapsWith(DateTime start, DateTime end)
    {
        return Start < end && End > start;
    }
}