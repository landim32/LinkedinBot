using Microsoft.Playwright;

namespace LinkedinBot.Infra.Interfaces.AppServices;

public interface ILinkedInAuthAppService
{
    Task EnsureLoggedInAsync(IPage page);
}
