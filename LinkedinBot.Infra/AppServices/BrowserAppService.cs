using LinkedinBot.DTO.Models;
using LinkedinBot.Infra.Interfaces.AppServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace LinkedinBot.Infra.AppServices;

public class BrowserAppService : IBrowserAppService
{
    private readonly BrowserSettings _settings;
    private readonly ILogger<BrowserAppService> _logger;
    private IPlaywright? _playwright;
    private IBrowserContext? _context;

    public BrowserAppService(IOptions<BrowserSettings> settings, ILogger<BrowserAppService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        _logger.LogInformation("Initializing Playwright browser...");

        _playwright = await Playwright.CreateAsync();

        var userDataPath = Path.GetFullPath(_settings.UserDataDir);
        Directory.CreateDirectory(userDataPath);

        _context = await _playwright.Chromium.LaunchPersistentContextAsync(
            userDataPath,
            new BrowserTypeLaunchPersistentContextOptions
            {
                Channel = _settings.Channel,
                Headless = _settings.Headless,
                Locale = _settings.Locale,
                TimezoneId = "America/Sao_Paulo",
                SlowMo = _settings.SlowMo,
                Args = new[]
                {
                    "--disable-blink-features=AutomationControlled",
                    "--no-sandbox"
                },
                ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36"
            });

        _logger.LogInformation("Browser initialized with locale {Locale} at {Path}", _settings.Locale, userDataPath);
    }

    public async Task<IPage> GetPageAsync()
    {
        if (_context is null)
            throw new InvalidOperationException("Browser not initialized. Call InitializeAsync first.");

        if (_context.Pages.Count > 0)
            return _context.Pages[0];

        return await _context.NewPageAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_context is not null)
        {
            _logger.LogInformation("Closing browser...");
            await _context.CloseAsync();
            _context = null;
        }

        _playwright?.Dispose();
        _playwright = null;

        GC.SuppressFinalize(this);
    }
}
