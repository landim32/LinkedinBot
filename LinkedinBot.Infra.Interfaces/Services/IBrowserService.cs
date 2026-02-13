using Microsoft.Playwright;

namespace LinkedinBot.Infra.Interfaces.Services;

public interface IBrowserService : IAsyncDisposable
{
    Task InitializeAsync();
    Task<IPage> GetPageAsync();
}
