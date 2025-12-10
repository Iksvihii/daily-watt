namespace DailyWatt.Application.DTO.Responses;

/// <summary>
/// Generic user profile DTO for application layer.
/// </summary>
public class UserProfileDto
{
  public string Email { get; set; } = string.Empty;
  public string Username { get; set; } = string.Empty;
}