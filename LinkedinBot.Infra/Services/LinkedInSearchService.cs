using System.Web;
using LinkedinBot.DTO.Models;
using LinkedinBot.Infra.Constants;
using LinkedinBot.Infra.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace LinkedinBot.Infra.Services;

public class LinkedInSearchService : ILinkedInSearchService
{
    private readonly JobSearchSettings _settings;
    private readonly ILogger<LinkedInSearchService> _logger;

    public LinkedInSearchService(IOptions<JobSearchSettings> settings, ILogger<LinkedInSearchService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<List<JobListing>> CollectJobsAsync(IPage page)
    {
        var url = BuildSearchUrl();
        _logger.LogInformation("Navigating to job search: {Url}", url);

        await page.GotoAsync(url, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 30000
        });

        await page.WaitForSelectorAsync(Selectors.JobCardContainer, new PageWaitForSelectorOptions
        {
            Timeout = 15000,
            State = WaitForSelectorState.Visible
        });

        await Task.Delay(2000);

        var jobs = new List<JobListing>();
        var processedJobUrls = new HashSet<string>();
        var currentPage = 1;

        while (jobs.Count < _settings.MaxApplicationsPerRun)
        {
            _logger.LogInformation("Processing search results page {Page}...", currentPage);

            var pageJobs = await ScrapeCurrentPageAsync(page, processedJobUrls);
            jobs.AddRange(pageJobs);

            _logger.LogInformation("Collected {Count} jobs so far (page {Page})", jobs.Count, currentPage);

            if (!await GoToNextPageAsync(page, currentPage))
                break;

            currentPage++;
            await Task.Delay(Random.Shared.Next(2000, 4000));
        }

        _logger.LogInformation("Total jobs collected: {Count}", jobs.Count);
        return jobs;
    }

    private async Task<List<JobListing>> ScrapeCurrentPageAsync(IPage page, HashSet<string> processedUrls)
    {
        var jobs = new List<JobListing>();

        // Wait for page to fully load
        _logger.LogInformation("Waiting for job list to load...");
        await Task.Delay(3000);

        // Scroll the job list container to load all lazy-loaded cards
        await ScrollJobListToBottomAsync(page);

        var jobCards = page.Locator(Selectors.JobCardContainer);
        var count = await jobCards.CountAsync();
        _logger.LogInformation("Found {Count} job cards on current page", count);

        for (var i = 0; i < count; i++)
        {
            try
            {
                var card = jobCards.Nth(i);

                await card.ScrollIntoViewIfNeededAsync();
                await card.ClickAsync();
                await Task.Delay(Random.Shared.Next(1500, 3000));

                await page.WaitForSelectorAsync(Selectors.JobDescription, new PageWaitForSelectorOptions
                {
                    Timeout = 10000,
                    State = WaitForSelectorState.Visible
                });

                var jobUrl = page.Url;

                if (processedUrls.Contains(jobUrl))
                    continue;

                processedUrls.Add(jobUrl);

                var job = await ScrapeJobDetailsAsync(page, jobUrl);
                if (job is not null)
                {
                    jobs.Add(job);
                    _logger.LogDebug("Scraped: {Title} at {Company}", job.Title, job.Company);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to scrape job card {Index}", i);
            }
        }

        return jobs;
    }

    private async Task ScrollJobListToBottomAsync(IPage page)
    {
        // LinkedIn uses occlusion: the <ul> contains ALL <li> items for the page,
        // but only the visible ones have rendered content (.job-card-container--clickable).
        // We need to find the actual scrollable element and scroll it to force rendering.

        var scaffoldList = page.Locator(Selectors.ScaffoldLayoutList);
        if (await scaffoldList.CountAsync() == 0)
        {
            _logger.LogWarning("scaffold-layout__list not found");
            return;
        }

        // Count total <li> items (rendered + occluded)
        var totalListItems = await scaffoldList.Locator("li.scaffold-layout__list-item").CountAsync();
        _logger.LogInformation("Total list items on page (rendered + occluded): {Total}", totalListItems);

        // Find the actual scrollable element by walking up from scaffold-layout__list
        // and checking each element + its children for scrollability
        var scrollableHandle = await page.EvaluateHandleAsync(@"() => {
            // Start from scaffold-layout__list and walk up to find scrollable ancestor
            const scaffoldList = document.querySelector('.scaffold-layout__list');
            if (!scaffoldList) return null;

            // Check if an element is scrollable
            function isScrollable(el) {
                const style = window.getComputedStyle(el);
                const overflowY = style.overflowY;
                const isOverflow = overflowY === 'auto' || overflowY === 'scroll';
                return isOverflow && el.scrollHeight > el.clientHeight;
            }

            // First check scaffold-layout__list itself
            if (isScrollable(scaffoldList)) return scaffoldList;

            // Check all children of scaffold-layout__list
            for (const child of scaffoldList.querySelectorAll('*')) {
                if (isScrollable(child)) return child;
            }

            // Walk up from scaffold-layout__list
            let el = scaffoldList.parentElement;
            while (el && el !== document.body) {
                if (isScrollable(el)) return el;
                el = el.parentElement;
            }

            // Fallback: return scaffold-layout__list itself
            return scaffoldList;
        }");

        var scrollable = scrollableHandle.AsElement();

        // Log which element was found
        var scrollInfo = await page.EvaluateAsync<string>(@"(el) => {
            const tag = el.tagName.toLowerCase();
            const cls = el.className.toString().substring(0, 80);
            const style = window.getComputedStyle(el);
            return `${tag}.${cls} | overflow-y=${style.overflowY} | scrollHeight=${el.scrollHeight} clientHeight=${el.clientHeight} scrollTop=${el.scrollTop}`;
        }", scrollable);
        _logger.LogInformation("Scrollable element found: {Info}", scrollInfo);

        // Now scroll this element
        var previousCardCount = 0;
        var noChangeRounds = 0;
        const int maxNoChangeRounds = 5;

        for (var scrollStep = 0; scrollStep < 80; scrollStep++)
        {
            var currentCardCount = await page.Locator(Selectors.JobCardContainer).CountAsync();

            _logger.LogInformation("Scroll step {Step}: {Cards}/{Total} cards rendered",
                scrollStep + 1, currentCardCount, totalListItems);

            if (currentCardCount >= totalListItems)
            {
                _logger.LogInformation("All {Count} cards rendered successfully", currentCardCount);
                break;
            }

            // Scroll down
            await page.EvaluateAsync("el => el.scrollBy(0, 400)", scrollable);
            await Task.Delay(1200);

            // Log scroll position
            var scrollPos = await page.EvaluateAsync<string>(
                "el => `scrollTop=${el.scrollTop} scrollHeight=${el.scrollHeight} clientHeight=${el.clientHeight}`",
                scrollable);
            _logger.LogInformation("Scroll position: {Pos}", scrollPos);

            if (currentCardCount == previousCardCount)
            {
                noChangeRounds++;
                if (noChangeRounds >= maxNoChangeRounds)
                {
                    _logger.LogInformation(
                        "No new cards after {Rounds} rounds, rendered {Cards}/{Total}",
                        maxNoChangeRounds, currentCardCount, totalListItems);
                    break;
                }
            }
            else
            {
                noChangeRounds = 0;
            }

            previousCardCount = currentCardCount;
        }

        // Scroll back to top
        await page.EvaluateAsync("el => el.scrollTop = 0", scrollable);
        await Task.Delay(1000);
    }

    private async Task<JobListing?> ScrapeJobDetailsAsync(IPage page, string jobUrl)
    {
        try
        {
            var title = await GetTextSafeAsync(page, Selectors.JobTitle);
            var company = await GetTextSafeAsync(page, Selectors.CompanyName);
            var description = await GetTextSafeAsync(page, Selectors.JobDescription);
            var location = await GetTextSafeAsync(page, Selectors.JobLocation);

            if (string.IsNullOrWhiteSpace(title))
                return null;

            var easyApplyButton = page.Locator(Selectors.EasyApplyButton);
            var isEasyApply = await easyApplyButton.CountAsync() > 0;

            var alreadyApplied = page.Locator(Selectors.AlreadyApplied);
            if (await alreadyApplied.CountAsync() > 0)
            {
                _logger.LogDebug("Skipping already applied job: {Title}", title);
                return null;
            }

            return new JobListing
            {
                Title = title.Trim(),
                Company = company.Trim(),
                Location = location.Trim(),
                Description = description.Trim(),
                JobUrl = jobUrl,
                IsEasyApply = isEasyApply
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error scraping job details at {Url}", jobUrl);
            return null;
        }
    }

    private async Task<bool> GoToNextPageAsync(IPage page, int currentPage)
    {
        try
        {
            var nextPageButton = page.Locator($"button[aria-label='Page {currentPage + 1}']");
            if (await nextPageButton.CountAsync() > 0)
            {
                await nextPageButton.ClickAsync();
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "No more pages available");
        }

        return false;
    }

    private string BuildSearchUrl()
    {
        var baseUrl = "https://www.linkedin.com/jobs/search/";
        var queryParams = new List<string>
        {
            $"keywords={HttpUtility.UrlEncode(_settings.Keywords)}",
            $"geoId={_settings.GeoId}",
            $"distance={_settings.Distance}",
            $"f_E={_settings.ExperienceLevel}",
            $"f_WT={_settings.RemoteFilter}"
        };

        if (_settings.EasyApply)
            queryParams.Add("f_AL=true");

        return $"{baseUrl}?{string.Join("&", queryParams)}";
    }

    public async Task DismissJobAsync(IPage page, string jobUrl)
    {
        try
        {
            // Navigate to the job if not already there
            if (!page.Url.Contains(jobUrl.Split('?')[0].Split('/').Last()))
            {
                await page.GotoAsync(jobUrl, new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.DOMContentLoaded,
                    Timeout = 15000
                });
                await Task.Delay(1500);
            }

            var dismissButton = page.Locator(Selectors.JobDismissButton);
            if (await dismissButton.CountAsync() > 0 && await dismissButton.First.IsVisibleAsync())
            {
                await dismissButton.First.ClickAsync();
                _logger.LogInformation("Dismissed incompatible job from feed");
                await Task.Delay(1000);
            }
            else
            {
                _logger.LogDebug("Dismiss button not found for job");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to dismiss job at {Url}", jobUrl);
        }
    }

    private static async Task<string> GetTextSafeAsync(IPage page, string selector)
    {
        try
        {
            var element = page.Locator(selector).First;
            if (await element.CountAsync() > 0)
                return await element.InnerTextAsync();
        }
        catch
        {
            // Element not found or not visible
        }

        return string.Empty;
    }
}
