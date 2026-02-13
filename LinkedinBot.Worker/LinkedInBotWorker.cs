using System.Text.Json;
using LinkedinBot.Domain.Services.Interfaces;
using LinkedinBot.DTO.Models;
using LinkedinBot.Infra.Interfaces.Services;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace LinkedinBot.Worker;

public class LinkedInBotWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<LinkedInBotWorker> _logger;

    public LinkedInBotWorker(
        IServiceProvider serviceProvider,
        ILogger<LinkedInBotWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("=== LinkedIn Easy Apply Worker Starting ===");

        // Singleton services — resolved once
        var browserService = _serviceProvider.GetRequiredService<IBrowserService>();
        var settings = _serviceProvider.GetRequiredService<IOptions<JobSearchSettings>>().Value;

        await browserService.InitializeAsync();
        var page = await browserService.GetPageAsync();

        // Initial load of history (first scope)
        using (var initScope = _serviceProvider.CreateScope())
        {
            var historyService = initScope.ServiceProvider.GetRequiredService<IJobHistoryService>();
            await historyService.LoadAsync();
        }

        // Ensure logged in
        using (var authScope = _serviceProvider.CreateScope())
        {
            var authService = authScope.ServiceProvider.GetRequiredService<ILinkedInAuthService>();
            await authService.EnsureLoggedInAsync(page);
        }

        var sessionResults = new List<ApplicationResult>();
        var cycleNumber = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            cycleNumber++;
            _logger.LogInformation("");
            _logger.LogInformation("========== CYCLE {Cycle} ==========", cycleNumber);

            try
            {
                // Each cycle gets a fresh scope (for scoped DbContext)
                using var scope = _serviceProvider.CreateScope();
                var provider = scope.ServiceProvider;

                var authService = provider.GetRequiredService<ILinkedInAuthService>();
                var searchService = provider.GetRequiredService<ILinkedInSearchService>();
                var applyService = provider.GetRequiredService<ILinkedInApplyService>();
                var analyzerService = provider.GetRequiredService<IJobAnalyzerService>();
                var historyService = provider.GetRequiredService<IJobHistoryService>();

                if (cycleNumber > 1)
                    await authService.EnsureLoggedInAsync(page);

                var allJobs = await searchService.CollectJobsAsync(page);
                _logger.LogInformation("Found {Count} jobs matching search criteria.", allJobs.Count);

                var newJobs = allJobs.Where(j => !historyService.IsJobAnalyzed(j.JobUrl)).ToList();
                _logger.LogInformation("{New} new jobs to analyze (skipped {Skipped} already analyzed).",
                    newJobs.Count, allJobs.Count - newJobs.Count);

                if (newJobs.Count == 0)
                {
                    _logger.LogInformation("No new jobs found this cycle.");
                    await WaitForNextCycle(settings, stoppingToken);
                    continue;
                }

                var appliedCount = sessionResults.Count(r => r.Status == ApplicationStatus.Applied);

                foreach (var job in newJobs)
                {
                    if (stoppingToken.IsCancellationRequested) break;
                    if (appliedCount >= settings.MaxApplicationsPerRun) break;

                    try
                    {
                        _logger.LogInformation("--- Processing: {Title} at {Company} ---", job.Title, job.Company);

                        var (shouldApply, compatibility) = await analyzerService.EvaluateJobAsync(job);

                        if (!shouldApply)
                        {
                            var result = new ApplicationResult
                            {
                                Job = job,
                                Status = ApplicationStatus.Incompatible,
                                Reason = compatibility.Reasoning
                            };
                            sessionResults.Add(result);
                            _logger.LogInformation("Skipped (incompatible): {Reason}", compatibility.Reasoning);

                            await historyService.SaveJobResultAsync(
                                ToHistoryEntry(job, result, compatibility));
                            await searchService.DismissJobAsync(page, job.JobUrl);
                            continue;
                        }

                        if (!job.IsEasyApply)
                        {
                            var result = new ApplicationResult
                            {
                                Job = job,
                                Status = ApplicationStatus.Skipped,
                                Reason = "Not an Easy Apply job"
                            };
                            sessionResults.Add(result);

                            await historyService.SaveJobResultAsync(
                                ToHistoryEntry(job, result, compatibility));
                            continue;
                        }

                        await page.GotoAsync(job.JobUrl, new PageGotoOptions
                        {
                            WaitUntil = WaitUntilState.DOMContentLoaded,
                            Timeout = 30000
                        });

                        await Task.Delay(Random.Shared.Next(2000, 4000), stoppingToken);

                        // No interactive prompt in Worker — pass null (skips unrecognized forms)
                        var success = await applyService.ApplyToJobAsync(page, job, null);

                        var appResult = new ApplicationResult
                        {
                            Job = job,
                            Status = success ? ApplicationStatus.Applied : ApplicationStatus.Failed,
                            AppliedAt = success ? DateTime.UtcNow : null,
                            Reason = success ? null : "Application process failed"
                        };
                        sessionResults.Add(appResult);

                        await historyService.SaveJobResultAsync(
                            ToHistoryEntry(job, appResult, compatibility));

                        if (success)
                        {
                            appliedCount++;
                            _logger.LogInformation(
                                "Successfully applied to: {Title} at {Company} ({Count}/{Max})",
                                job.Title, job.Company, appliedCount, settings.MaxApplicationsPerRun);
                        }

                        await Task.Delay(Random.Shared.Next(3000, 7000), stoppingToken);
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing job: {Title} at {Company}", job.Title, job.Company);

                        var failResult = new ApplicationResult
                        {
                            Job = job,
                            Status = ApplicationStatus.Failed,
                            Reason = ex.Message
                        };
                        sessionResults.Add(failResult);
                        await historyService.SaveJobResultAsync(ToHistoryEntry(job, failResult));
                    }
                }

                PrintCycleSummary(cycleNumber, sessionResults, allJobs.Count);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Cycle {Cycle} interrupted by shutdown.", cycleNumber);
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during cycle {Cycle}. Will retry next cycle.", cycleNumber);
            }

            await WaitForNextCycle(settings, stoppingToken);
        }

        PrintSessionSummary(sessionResults, cycleNumber);

        await browserService.DisposeAsync();
        _logger.LogInformation("Worker shutdown complete.");
    }

    private async Task WaitForNextCycle(JobSearchSettings settings, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Next cycle in {Minutes} minute(s)...", settings.SearchIntervalMinutes);
            await Task.Delay(TimeSpan.FromMinutes(settings.SearchIntervalMinutes), ct);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Wait interrupted by shutdown.");
        }
    }

    private void PrintCycleSummary(int cycleNumber, List<ApplicationResult> results, int totalJobsFound)
    {
        _logger.LogInformation("");
        _logger.LogInformation("--- Cycle {Cycle} Summary ---", cycleNumber);
        _logger.LogInformation("Total jobs found:    {Total}", totalJobsFound);
        _logger.LogInformation("Applied:             {Applied}", results.Count(r => r.Status == ApplicationStatus.Applied));
        _logger.LogInformation("Incompatible:        {Incompatible}", results.Count(r => r.Status == ApplicationStatus.Incompatible));
        _logger.LogInformation("Skipped:             {Skipped}", results.Count(r => r.Status == ApplicationStatus.Skipped));
        _logger.LogInformation("Failed:              {Failed}", results.Count(r => r.Status == ApplicationStatus.Failed));
    }

    private void PrintSessionSummary(List<ApplicationResult> results, int totalCycles)
    {
        _logger.LogInformation("");
        _logger.LogInformation("========== SESSION SUMMARY ==========");
        _logger.LogInformation("Total cycles:        {Cycles}", totalCycles);
        _logger.LogInformation("Total jobs analyzed: {Total}", results.Count);
        _logger.LogInformation("Applied:             {Applied}", results.Count(r => r.Status == ApplicationStatus.Applied));
        _logger.LogInformation("Incompatible:        {Incompatible}", results.Count(r => r.Status == ApplicationStatus.Incompatible));
        _logger.LogInformation("Skipped:             {Skipped}", results.Count(r => r.Status == ApplicationStatus.Skipped));
        _logger.LogInformation("Failed:              {Failed}", results.Count(r => r.Status == ApplicationStatus.Failed));
        _logger.LogInformation("=====================================");
    }

    private static JobHistoryEntry ToHistoryEntry(
        JobListing job, ApplicationResult result, CompatibilityResult? compatibility = null)
    {
        return new JobHistoryEntry
        {
            JobUrl = job.JobUrl,
            Title = job.Title,
            Company = job.Company,
            Location = job.Location,
            Description = job.Description,
            Status = result.Status.ToString(),
            Reason = result.Reason,
            ConfidenceScore = compatibility?.ConfidenceScore,
            AiMessage = compatibility?.Reasoning,
            KeyMatchingSkills = compatibility?.KeyMatchingSkills is { Count: > 0 }
                ? JsonSerializer.Serialize(compatibility.KeyMatchingSkills) : null,
            MissingRequirements = compatibility?.MissingRequirements is { Count: > 0 }
                ? JsonSerializer.Serialize(compatibility.MissingRequirements) : null,
            AnalyzedAt = DateTime.UtcNow,
            AppliedAt = result.AppliedAt
        };
    }
}
