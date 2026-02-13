using LinkedinBot.DTO.Models;

namespace LinkedinBot.Infra.Interfaces.Services;

public interface IJobHistoryService
{
    Task LoadAsync();
    bool IsJobAnalyzed(string jobUrl);
    Task SaveJobResultAsync(JobHistoryEntry entry);
    Task<List<JobHistoryEntry>> GetHistoryAsync();
}
