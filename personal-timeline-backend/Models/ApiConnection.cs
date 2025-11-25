namespace personal_timeline_backend.Models;

public class ApiConnection
{
 public int Id { get; set; }
 public int UserId { get; set; }
 public string ApiProvider { get; set; } = string.Empty;
 public string AccessToken { get; set; } = string.Empty;
 public string? RefreshToken { get; set; } = string.Empty;
 public DateTime TokenExpiresAt { get; set; }
 public DateTime LastSyncAt { get; set; }
 public bool IsActive { get; set; }
 public string Settings { get; set; } = string.Empty; // JSON for API-specific settings

 // Navigation properties
 public User User { get; set; } = null!;
}
