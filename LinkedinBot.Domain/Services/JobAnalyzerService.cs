using LinkedinBot.Domain.Services.Interfaces;
using LinkedinBot.DTO.Models;
using Microsoft.Extensions.Logging;

namespace LinkedinBot.Domain.Services;

public class JobAnalyzerService : IJobAnalyzerService
{
    private readonly IChatGptService _chatGptService;
    private readonly ILogger<JobAnalyzerService> _logger;
    private const double CompatibilityThreshold = 0.6;

    public JobAnalyzerService(IChatGptService chatGptService, ILogger<JobAnalyzerService> logger)
    {
        _chatGptService = chatGptService;
        _logger = logger;
    }

    public async Task<(bool ShouldApply, CompatibilityResult Result)> EvaluateJobAsync(JobListing job)
    {
        _logger.LogInformation("Evaluating job: {Title} at {Company}", job.Title, job.Company);

        var result = await _chatGptService.AnalyzeJobCompatibilityAsync(job);

        var shouldApply = result.IsCompatible && result.ConfidenceScore >= CompatibilityThreshold;

        _logger.LogInformation(
            "Job: {Title} | Compatible: {Compatible} | Score: {Score:P0} | Apply: {ShouldApply} | Reason: {Reasoning}",
            job.Title,
            result.IsCompatible,
            result.ConfidenceScore,
            shouldApply,
            result.Reasoning);

        if (result.KeyMatchingSkills.Count > 0)
            _logger.LogDebug("Matching skills: {Skills}", string.Join(", ", result.KeyMatchingSkills));

        if (result.MissingRequirements.Count > 0)
            _logger.LogDebug("Missing requirements: {Missing}", string.Join(", ", result.MissingRequirements));

        return (shouldApply, result);
    }
}
