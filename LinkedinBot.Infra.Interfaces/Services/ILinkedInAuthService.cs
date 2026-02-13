using Microsoft.Playwright;

namespace LinkedinBot.Infra.Interfaces.Services;

public interface ILinkedInAuthService
{
    Task EnsureLoggedInAsync(IPage page);
}
