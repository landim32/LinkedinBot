using LinkedinBot.DTO.Models;
using Microsoft.Playwright;

namespace LinkedinBot.Infra.Interfaces.Services;

public interface ILinkedInApplyService
{
    Task<bool> ApplyToJobAsync(IPage page, JobListing job, Func<int, bool>? onUnrecognizedAction = null);
}
