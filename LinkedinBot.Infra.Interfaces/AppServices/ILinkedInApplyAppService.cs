using LinkedinBot.DTO.Models;
using Microsoft.Playwright;

namespace LinkedinBot.Infra.Interfaces.AppServices;

public interface ILinkedInApplyAppService
{
    Task<bool> ApplyToJobAsync(IPage page, JobListing job, Func<int, bool>? onUnrecognizedAction = null);
}
