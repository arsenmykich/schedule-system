namespace ScheduleSystem.Models;

/// <summary>
/// Represents a user in the scheduling system
/// </summary>
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    
    // Navigation property for meetings
    public virtual ICollection<Meeting> Meetings { get; set; } = new List<Meeting>();
}