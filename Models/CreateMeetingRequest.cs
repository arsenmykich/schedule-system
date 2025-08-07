using System.ComponentModel.DataAnnotations;

namespace ScheduleSystem.Models;

/// <summary>
/// Request model for scheduling a new meeting
/// </summary>
public class CreateMeetingRequest
{
    [Required(ErrorMessage = "Participant IDs are required")]
    [MinLength(1, ErrorMessage = "At least one participant is required")]
    public List<int> ParticipantIds { get; set; } = new List<int>();

    [Range(1, 480, ErrorMessage = "Duration must be between 1 and 480 minutes")]
    public int DurationMinutes { get; set; }

    [Required(ErrorMessage = "Earliest start time is required")]
    public DateTime EarliestStart { get; set; }

    [Required(ErrorMessage = "Latest end time is required")]
    public DateTime LatestEnd { get; set; }
}