using LinkedinBot.DTO.Models;
using Microsoft.Playwright;

namespace LinkedinBot.Infra.Interfaces.AppServices;

public interface ILinkedInSearchAppService
{
    Task<List<JobListing>> CollectJobsAsync(IPage page);
    Task DismissJobAsync(IPage page, string jobUrl);
}
