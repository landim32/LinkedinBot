namespace LinkedinBot.DTO.Models;

public class JobHistoryEntry
{
    public string JobUrl { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public DateTime AnalyzedAt { get; set; }
    public DateTime? AppliedAt { get; set; }
}
