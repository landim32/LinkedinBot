using LinkedinBot.DTO.Models;

namespace LinkedinBot.Domain.Services.Interfaces;

public interface IChatGptService
{
    Task<CompatibilityResult> AnalyzeJobCompatibilityAsync(JobListing job);
    Task<string> AnswerFormQuestionAsync(string question, List<string>? options = null);
}
