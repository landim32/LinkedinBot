using LinkedinBot.DTO.Models;
using LinkedinBot.Infra.Interfaces.AppServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace LinkedinBot.Infra.AppServices;

public class LinkedInAuthAppService : ILinkedInAuthAppService
{
    private readonly LinkedInSettings _settings;
    private readonly ILogger<LinkedInAuthAppService> _logger;

    public LinkedInAuthAppService(IOptions<LinkedInSettings> settings, ILogger<LinkedInAuthAppService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task EnsureLoggedInAsync(IPage page)
    {
        _logger.LogInformation("Checking LinkedIn login status...");

        await page.GotoAsync("https://www.linkedin.com/feed/", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 30000
        });

        if (await IsLoggedInAsync(page))
        {
            _logger.LogInformation("Already logged in to LinkedIn.");
            return;
        }

        _logger.LogInformation("Not logged in. Performing login...");
        await PerformLoginAsync(page);
    }

    private async Task<bool> IsLoggedInAsync(IPage page)
    {
        try
        {
            await page.WaitForURLAsync("**/feed/**", new PageWaitForURLOptions { Timeout = 5000 });
            return true;
        }
        catch (TimeoutException)
        {
            return false;
        }
    }

    private async Task PerformLoginAsync(IPage page)
    {
        await page.GotoAsync("https://www.linkedin.com/login", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded
        });

        var emailInput = page.Locator("#username");
        var passwordInput = page.Locator("#password");

        await emailInput.WaitForAsync(new LocatorWaitForOptions { Timeout = 10000 });
        await emailInput.FillAsync(_settings.Email);
        await passwordInput.FillAsync(_settings.Password);

        _logger.LogInformation("Credentials entered. Clicking sign in...");

        var signInButton = page.Locator("button[type='submit']");
        await signInButton.ClickAsync();

        try
        {
            await page.WaitForURLAsync("**/feed/**", new PageWaitForURLOptions { Timeout = 15000 });
            _logger.LogInformation("Login successful!");
            return;
        }
        catch (TimeoutException)
        {
            _logger.LogWarning(
                "Login did not redirect to feed. A security verification may be required. " +
                "Please complete the verification in the browser window. Waiting up to 120 seconds...");
        }

        try
        {
            await page.WaitForURLAsync("**/feed/**", new PageWaitForURLOptions { Timeout = 120000 });
            _logger.LogInformation("Login successful after verification!");
        }
        catch (TimeoutException)
        {
            throw new InvalidOperationException(
                "Login failed. Could not reach the LinkedIn feed after 120 seconds. " +
                "Please check credentials or complete any pending verification.");
        }
    }
}
