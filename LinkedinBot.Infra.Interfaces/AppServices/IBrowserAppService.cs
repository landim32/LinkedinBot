using Microsoft.Playwright;

namespace LinkedinBot.Infra.Interfaces.AppServices;

public interface IBrowserAppService : IAsyncDisposable
{
    Task InitializeAsync();
    Task<IPage> GetPageAsync();
}
