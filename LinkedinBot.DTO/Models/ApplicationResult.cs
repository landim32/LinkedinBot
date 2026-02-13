namespace LinkedinBot.DTO.Models;

public class ApplicationResult
{
    public JobListing Job { get; set; } = null!;
    public ApplicationStatus Status { get; set; }
    public string? Reason { get; set; }
    public DateTime? AppliedAt { get; set; }
}

public enum ApplicationStatus
{
    Applied,
    Skipped,
    Failed,
    Incompatible
}
