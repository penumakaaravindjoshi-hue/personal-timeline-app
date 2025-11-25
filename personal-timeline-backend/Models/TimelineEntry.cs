namespace personal_timeline_backend.Models;

public class TimelineEntry
{
 public int Id { get; set; }
 public int UserId { get; set; }
 public string Title { get; set; } = string.Empty;
 public string? Description { get; set; }
 public DateTime EventDate { get; set; }
 public string EntryType { get; set; } = string.Empty; // Achievement, Activity, Milestone, Memory
 public string? Category { get; set; }
 public string? ImageUrl { get; set; }
 public string? ExternalUrl { get; set; }
 public string? SourceApi { get; set; } // Which API this came from
 public string? ExternalId { get; set; } // ID from external API
 public string? Metadata { get; set; } // JSON for additional data
 public DateTime CreatedAt { get; set; }
 public DateTime UpdatedAt { get; set; }
 

 // Navigation properties
 public User? User { get; set; }
}
