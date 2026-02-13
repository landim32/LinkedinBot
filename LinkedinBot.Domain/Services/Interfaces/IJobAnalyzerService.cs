using LinkedinBot.DTO.Models;

namespace LinkedinBot.Domain.Services.Interfaces;

public interface IJobAnalyzerService
{
    Task<(bool ShouldApply, CompatibilityResult Result)> EvaluateJobAsync(JobListing job);
}
