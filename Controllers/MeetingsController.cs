using Microsoft.AspNetCore.Mvc;
using ScheduleSystem.Models;
using ScheduleSystem.Services;

namespace ScheduleSystem.Controllers;

/// <summary>
/// Controller for managing meeting scheduling
/// </summary>
[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class MeetingsController : ControllerBase
{
    private readonly IMeetingSchedulingService _meetingService;
    private readonly ILogger<MeetingsController> _logger;

    public MeetingsController(IMeetingSchedulingService meetingService, ILogger<MeetingsController> logger)
    {
        _meetingService = meetingService;
        _logger = logger;
    }

    /// <summary>
    /// Schedules a new meeting by finding the earliest available time slot
    /// </summary>
    /// <param name="request">Meeting scheduling request</param>
    /// <returns>Scheduled meeting details or null if no slot available</returns>
    /// <response code="201">Meeting scheduled successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="409">No available time slot found</response>
    [HttpPost]
    [ProducesResponseType(typeof(MeetingResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MeetingResponse>> ScheduleMeeting([FromBody] CreateMeetingRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var meeting = await _meetingService.ScheduleMeetingAsync(request);
            
            if (meeting == null)
            {
                _logger.LogWarning("No available time slot found for meeting request with participants {ParticipantIds}", 
                    string.Join(", ", request.ParticipantIds));
                return Conflict("No available time slot found for the requested parameters");
            }

            _logger.LogInformation("Scheduled meeting with ID {MeetingId} from {StartTime} to {EndTime} with participants {ParticipantIds}",
                meeting.Id, meeting.StartTime, meeting.EndTime, string.Join(", ", meeting.ParticipantIds));

            return CreatedAtAction(nameof(GetMeeting), new { id = meeting.Id }, meeting);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid meeting request: {Message}", ex.Message);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scheduling meeting with participants {ParticipantIds}", 
                string.Join(", ", request.ParticipantIds));
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets all scheduled meetings
    /// </summary>
    /// <returns>List of all meetings</returns>
    /// <response code="200">Meetings retrieved successfully</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<MeetingResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<MeetingResponse>>> GetAllMeetings()
    {
        try
        {
            var meetings = await _meetingService.GetAllMeetingsAsync();
            return Ok(meetings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all meetings");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets a specific meeting by ID
    /// </summary>
    /// <param name="id">Meeting ID</param>
    /// <returns>Meeting details</returns>
    /// <response code="200">Meeting found</response>
    /// <response code="404">Meeting not found</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(MeetingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MeetingResponse>> GetMeeting(int id)
    {
        try
        {
            var meeting = await _meetingService.GetMeetingByIdAsync(id);
            if (meeting == null)
            {
                return NotFound($"Meeting with ID {id} not found");
            }

            return Ok(meeting);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving meeting with ID {MeetingId}", id);
            return StatusCode(500, "Internal server error");
        }
    }
}