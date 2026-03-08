using LinkedinBot.DTO.Models;

namespace LinkedinBot.Infra.Interfaces.AppServices;

public interface IJobHistoryAppService
{
    Task LoadAsync();
    bool IsJobAnalyzed(string jobUrl);
    Task SaveJobResultAsync(JobHistoryEntry entry);
    Task<List<JobHistoryEntry>> GetHistoryAsync();
}
