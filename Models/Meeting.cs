namespace ScheduleSystem.Models;

/// <summary>
/// Represents a scheduled meeting
/// </summary>
public class Meeting
{
    public int Id { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    
    // Navigation properties
    public virtual ICollection<User> Participants { get; set; } = new List<User>();
    public List<int> ParticipantIds { get; set; } = new List<int>();
}