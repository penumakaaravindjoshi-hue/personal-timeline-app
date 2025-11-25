using System.Collections.Generic;

namespace personal_timeline_backend.Models;

public class User
{
 public int Id { get; set; }
 public string OAuthProvider { get; set; } = string.Empty;
 public string OAuthId { get; set; } = string.Empty;
 public string Email { get; set; } = string.Empty;
 public string DisplayName { get; set; } = string.Empty;
 public string ProfileImageUrl { get; set; } = string.Empty;
 public DateTime CreatedAt { get; set; }
 public DateTime LastLoginAt { get; set; }

 // Navigation properties
 public ICollection<TimelineEntry> TimelineEntries { get; set; } = new List<TimelineEntry>();
 public ICollection<ApiConnection> ApiConnections { get; set; } = new List<ApiConnection>();
}
