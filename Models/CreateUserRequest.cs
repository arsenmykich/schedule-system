using System.ComponentModel.DataAnnotations;

namespace ScheduleSystem.Models;

/// <summary>
/// Request model for creating a new user
/// </summary>
public class CreateUserRequest
{
    [Required(ErrorMessage = "Name is required")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 100 characters")]
    public string Name { get; set; } = string.Empty;
}