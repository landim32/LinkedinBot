namespace LinkedinBot.DTO.Models;

public class JobListing
{
    public string Title { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string JobUrl { get; set; } = string.Empty;
    public bool IsEasyApply { get; set; }
    public string PostedDate { get; set; } = string.Empty;
    public string? ApplicantCount { get; set; }
}
