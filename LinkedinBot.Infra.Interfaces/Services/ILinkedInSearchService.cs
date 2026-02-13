using LinkedinBot.DTO.Models;
using Microsoft.Playwright;

namespace LinkedinBot.Infra.Interfaces.Services;

public interface ILinkedInSearchService
{
    Task<List<JobListing>> CollectJobsAsync(IPage page);
    Task DismissJobAsync(IPage page, string jobUrl);
}
