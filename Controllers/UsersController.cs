using Microsoft.AspNetCore.Mvc;
using ScheduleSystem.Models;
using ScheduleSystem.Services;

namespace ScheduleSystem.Controllers;

/// <summary>
/// Controller for managing users
/// </summary>
[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUserService userService, ILogger<UsersController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new user
    /// </summary>
    /// <param name="request">User creation request</param>
    /// <returns>Created user</returns>
    /// <response code="201">User created successfully</response>
    /// <response code="400">Invalid request data</response>
    [HttpPost]
    [ProducesResponseType(typeof(User), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<User>> CreateUser([FromBody] CreateUserRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _userService.CreateUserAsync(request);
            _logger.LogInformation("Created user with ID {UserId} and name {UserName}", user.Id, user.Name);
            
            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user with name {UserName}", request.Name);
            return BadRequest($"Error creating user: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets all users
    /// </summary>
    /// <returns>List of all users</returns>
    /// <response code="200">Users retrieved successfully</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<User>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<User>>> GetAllUsers()
    {
        try
        {
            var users = await _userService.GetAllUsersAsync();
            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all users");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets a specific user by ID
    /// </summary>
    /// <param name="id">User ID</param>
    /// <returns>User details</returns>
    /// <response code="200">User found</response>
    /// <response code="404">User not found</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(User), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<User>> GetUser(int id)
    {
        try
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound($"User with ID {id} not found");
            }

            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user with ID {UserId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets all meetings for a specific user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>List of meetings for the user</returns>
    /// <response code="200">Meetings retrieved successfully</response>
    /// <response code="404">User not found</response>
    [HttpGet("{userId}/meetings")]
    [ProducesResponseType(typeof(IEnumerable<MeetingResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<MeetingResponse>>> GetUserMeetings(int userId)
    {
        try
        {
            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null)
            {
                return NotFound($"User with ID {userId} not found");
            }

            var meetings = await _userService.GetUserMeetingsAsync(userId);
            var meetingResponses = meetings.Select(m => new MeetingResponse
            {
                Id = m.Id,
                StartTime = m.StartTime,
                EndTime = m.EndTime,
                ParticipantIds = m.ParticipantIds,
                ParticipantNames = m.Participants.Select(p => p.Name).ToList()
            });

            return Ok(meetingResponses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving meetings for user {UserId}", userId);
            return StatusCode(500, "Internal server error");
        }
    }
}