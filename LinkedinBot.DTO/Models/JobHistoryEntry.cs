namespace LinkedinBot.DTO.Models;

public class JobHistoryEntry
{
    public int Id { get; set; }
    public string JobUrl { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public double? ConfidenceScore { get; set; }
    public string? AiMessage { get; set; }
    public string? KeyMatchingSkills { get; set; }
    public string? MissingRequirements { get; set; }
    public DateTime AnalyzedAt { get; set; }
    public DateTime? AppliedAt { get; set; }
}
