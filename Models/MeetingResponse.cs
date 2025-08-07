namespace ScheduleSystem.Models;

/// <summary>
/// Response model for meeting operations
/// </summary>
public class MeetingResponse
{
    public int Id { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public List<int> ParticipantIds { get; set; } = new List<int>();
    public List<string> ParticipantNames { get; set; } = new List<string>();
    public int DurationMinutes => (int)(EndTime - StartTime).TotalMinutes;
}