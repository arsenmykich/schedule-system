using Microsoft.EntityFrameworkCore;
using ScheduleSystem.Data;
using ScheduleSystem.Models;

namespace ScheduleSystem.Services;

/// <summary>
/// Service for scheduling meetings with conflict detection algorithm
/// </summary>
public class MeetingSchedulingService : IMeetingSchedulingService
{
    private readonly ScheduleDbContext _context;
    private readonly IUserService _userService;

    // Business hours constants
    private static readonly TimeOnly BusinessStartTime = new(9, 0);  // 9:00 AM
    private static readonly TimeOnly BusinessEndTime = new(17, 0);   // 5:00 PM

    public MeetingSchedulingService(ScheduleDbContext context, IUserService userService)
    {
        _context = context;
        _userService = userService;
    }

    public async Task<MeetingResponse?> ScheduleMeetingAsync(CreateMeetingRequest request)
    {
        // Validate participants exist
        if (!await _userService.UsersExistAsync(request.ParticipantIds))
        {
            throw new ArgumentException("One or more participant IDs are invalid");
        }

        // Validate time constraints
        if (request.EarliestStart >= request.LatestEnd)
        {
            throw new ArgumentException("Earliest start time must be before latest end time");
        }

        var duration = TimeSpan.FromMinutes(request.DurationMinutes);
        if (request.EarliestStart.Add(duration) > request.LatestEnd)
        {
            return null; // Changed to return null instead of throwing exception
        }

        // Find the earliest available time slot
        var availableSlot = await FindEarliestAvailableSlotAsync(
            request.ParticipantIds,
            duration,
            request.EarliestStart,
            request.LatestEnd
        );

        if (availableSlot == null)
        {
            return null; // No available slot found
        }

        // Create the meeting
        var meeting = new Meeting
        {
            StartTime = availableSlot.Start,
            EndTime = availableSlot.End,
            ParticipantIds = request.ParticipantIds
        };

        // Load participants for the relationship
        var participants = await _context.Users
            .Where(u => request.ParticipantIds.Contains(u.Id))
            .ToListAsync();

        meeting.Participants = participants;

        _context.Meetings.Add(meeting);
        await _context.SaveChangesAsync();

        return new MeetingResponse
        {
            Id = meeting.Id,
            StartTime = meeting.StartTime,
            EndTime = meeting.EndTime,
            ParticipantIds = meeting.ParticipantIds,
            ParticipantNames = participants.Select(p => p.Name).ToList()
        };
    }

    /// <summary>
    /// Core algorithm: Finds the earliest available time slot for all participants
    /// </summary>
    private async Task<TimeSlot?> FindEarliestAvailableSlotAsync(
        List<int> participantIds,
        TimeSpan duration,
        DateTime earliestStart,
        DateTime latestEnd)
    {
        // Get all existing meetings for the participants within the date range
        var existingMeetings = await GetConflictingMeetingsAsync(participantIds, earliestStart, latestEnd);

        // Create sorted list of all busy time slots
        var busySlots = existingMeetings
            .Select(m => new TimeSlot { Start = m.StartTime, End = m.EndTime })
            .OrderBy(slot => slot.Start)
            .ToList();

        // Merge overlapping busy slots to optimize the algorithm
        var mergedBusySlots = MergeOverlappingSlots(busySlots);

        // Find the earliest available slot
        return FindFirstAvailableSlot(mergedBusySlots, duration, earliestStart, latestEnd);
    }

    /// <summary>
    /// Gets all meetings that could conflict with the requested time range for any participant
    /// </summary>
    private async Task<List<Meeting>> GetConflictingMeetingsAsync(
        List<int> participantIds,
        DateTime earliestStart,
        DateTime latestEnd)
    {
        return await _context.Meetings
            .Where(m => m.Participants.Any(p => participantIds.Contains(p.Id)) &&
                       m.StartTime < latestEnd &&
                       m.EndTime > earliestStart)
            .Include(m => m.Participants)
            .ToListAsync();
    }

    /// <summary>
    /// Merges overlapping time slots to reduce complexity
    /// </summary>
    private List<TimeSlot> MergeOverlappingSlots(List<TimeSlot> slots)
    {
        if (!slots.Any())
            return new List<TimeSlot>();

        var merged = new List<TimeSlot>();
        var current = slots[0];

        for (int i = 1; i < slots.Count; i++)
        {
            var next = slots[i];
            
            // If current slot overlaps or is adjacent to next slot, merge them
            if (current.End >= next.Start)
            {
                current.End = current.End > next.End ? current.End : next.End;
            }
            else
            {
                // No overlap, add current to merged list and move to next
                merged.Add(current);
                current = next;
            }
        }

        merged.Add(current);
        return merged;
    }

    /// <summary>
    /// Finds the first available time slot that satisfies all constraints
    /// </summary>
    private TimeSlot? FindFirstAvailableSlot(
        List<TimeSlot> busySlots,
        TimeSpan duration,
        DateTime earliestStart,
        DateTime latestEnd)
    {
        // Ensure we respect business hours
        var searchStart = EnsureBusinessHours(earliestStart);
        var searchEnd = EnsureBusinessHours(latestEnd);

        if (searchStart >= searchEnd || searchStart.Add(duration) > searchEnd)
        {
            return null;
        }

        var currentTime = searchStart;

        foreach (var busySlot in busySlots)
        {
            // Check if there's a gap before this busy slot
            if (currentTime.Add(duration) <= busySlot.Start)
            {
                var proposedEnd = currentTime.Add(duration);
                
                // Ensure the proposed slot is within business hours and search bounds
                if (IsWithinBusinessHours(currentTime, proposedEnd) && proposedEnd <= searchEnd)
                {
                    return new TimeSlot { Start = currentTime, End = proposedEnd };
                }
            }

            // Move current time to after this busy slot, but ensure business hours
            currentTime = EnsureBusinessHours(busySlot.End);
            
            // If we've moved past the search end, no slot is available
            if (currentTime >= searchEnd)
            {
                break;
            }
        }

        // Check if there's space after the last busy slot
        if (currentTime.Add(duration) <= searchEnd && IsWithinBusinessHours(currentTime, currentTime.Add(duration)))
        {
            return new TimeSlot { Start = currentTime, End = currentTime.Add(duration) };
        }

        return null; // No available slot found
    }

    /// <summary>
    /// Ensures a DateTime falls within business hours
    /// </summary>
    private DateTime EnsureBusinessHours(DateTime dateTime)
    {
        var date = DateOnly.FromDateTime(dateTime);
        var time = TimeOnly.FromDateTime(dateTime);

        if (time < BusinessStartTime)
        {
            return date.ToDateTime(BusinessStartTime);
        }
        else if (time > BusinessEndTime)
        {
            // Move to next day's business start
            return date.AddDays(1).ToDateTime(BusinessStartTime);
        }

        return dateTime;
    }

    /// <summary>
    /// Checks if a time slot is entirely within business hours
    /// </summary>
    private bool IsWithinBusinessHours(DateTime start, DateTime end)
    {
        // Check if both times are on the same day
        if (start.Date != end.Date)
        {
            return false;
        }

        var startTime = TimeOnly.FromDateTime(start);
        var endTime = TimeOnly.FromDateTime(end);

        return startTime >= BusinessStartTime && endTime <= BusinessEndTime;
    }

    public async Task<IEnumerable<MeetingResponse>> GetAllMeetingsAsync()
    {
        var meetings = await _context.Meetings
            .Include(m => m.Participants)
            .ToListAsync();

        return meetings.Select(m => new MeetingResponse
        {
            Id = m.Id,
            StartTime = m.StartTime,
            EndTime = m.EndTime,
            ParticipantIds = m.ParticipantIds,
            ParticipantNames = m.Participants.Select(p => p.Name).ToList()
        });
    }

    public async Task<MeetingResponse?> GetMeetingByIdAsync(int id)
    {
        var meeting = await _context.Meetings
            .Include(m => m.Participants)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (meeting == null)
            return null;

        return new MeetingResponse
        {
            Id = meeting.Id,
            StartTime = meeting.StartTime,
            EndTime = meeting.EndTime,
            ParticipantIds = meeting.ParticipantIds,
            ParticipantNames = meeting.Participants.Select(p => p.Name).ToList()
        };
    }
}