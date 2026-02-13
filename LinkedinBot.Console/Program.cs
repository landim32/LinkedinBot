using LinkedinBot.Application;
using LinkedinBot.Domain.Services.Interfaces;
using LinkedinBot.DTO.Models;
using LinkedinBot.Infra.Interfaces.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

using var cts = new CancellationTokenSource();

Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    Log.Information("Ctrl+C detected. Finishing current cycle and shutting down...");
    cts.Cancel();
};

var sessionResults = new List<ApplicationResult>();
var sessionStartedAt = DateTime.UtcNow;

try
{
    Log.Information("=== LinkedIn Easy Apply Bot ===");
    Log.Information("Press Ctrl+C to stop gracefully.");

    var host = Host.CreateDefaultBuilder(args)
        .UseSerilog((context, services, loggerConfig) =>
        {
            loggerConfig.ReadFrom.Configuration(context.Configuration);
        })
        .ConfigureServices((context, services) =>
        {
            services.RegisterServices(context.Configuration);
        })
        .Build();

    using var scope = host.Services.CreateScope();
    var provider = scope.ServiceProvider;

    var browserService = provider.GetRequiredService<IBrowserService>();
    var authService = provider.GetRequiredService<ILinkedInAuthService>();
    var searchService = provider.GetRequiredService<ILinkedInSearchService>();
    var applyService = provider.GetRequiredService<ILinkedInApplyService>();
    var analyzerService = provider.GetRequiredService<IJobAnalyzerService>();
    var historyService = provider.GetRequiredService<IJobHistoryService>();
    var settings = provider.GetRequiredService<IOptions<JobSearchSettings>>().Value;

    // 1. Initialize browser and load history
    await browserService.InitializeAsync();
    var page = await browserService.GetPageAsync();
    await historyService.LoadAsync();

    // 2. Ensure logged in
    await authService.EnsureLoggedInAsync(page);

    var cycleNumber = 0;

    // 3. Main loop
    while (!cts.Token.IsCancellationRequested)
    {
        cycleNumber++;
        Log.Information("");
        Log.Information("========== CYCLE {Cycle} ==========", cycleNumber);

        try
        {
            // 3a. Re-check login (session may expire in long runs)
            if (cycleNumber > 1)
            {
                await authService.EnsureLoggedInAsync(page);
            }

            // 3b. Search and collect jobs
            var allJobs = await searchService.CollectJobsAsync(page);
            Log.Information("Found {Count} jobs matching search criteria.", allJobs.Count);

            // 3c. Filter out already-analyzed jobs
            var newJobs = allJobs.Where(j => !historyService.IsJobAnalyzed(j.JobUrl)).ToList();
            Log.Information("{New} new jobs to analyze (skipped {Skipped} already analyzed).",
                newJobs.Count, allJobs.Count - newJobs.Count);

            if (newJobs.Count == 0)
            {
                Log.Information("No new jobs found this cycle.");
                PrintCycleSummary(cycleNumber, [], allJobs.Count);
                await WaitForNextCycle(settings.SearchIntervalMinutes, cts.Token);
                continue;
            }

            // 3d. Process each new job
            var cycleResults = new List<ApplicationResult>();
            var appliedCount = sessionResults.Count(r => r.Status == ApplicationStatus.Applied);

            foreach (var job in newJobs)
            {
                if (cts.Token.IsCancellationRequested)
                {
                    Log.Information("Shutdown requested. Finishing current cycle...");
                    break;
                }

                if (appliedCount >= settings.MaxApplicationsPerRun)
                {
                    Log.Information("Reached max applications per run ({Max}). Waiting for next cycle.",
                        settings.MaxApplicationsPerRun);
                    break;
                }

                try
                {
                    Log.Information("--- Processing: {Title} at {Company} ---", job.Title, job.Company);

                    // Analyze compatibility with ChatGPT
                    var (shouldApply, compatibility) = await analyzerService.EvaluateJobAsync(job);

                    if (!shouldApply)
                    {
                        var result = new ApplicationResult
                        {
                            Job = job,
                            Status = ApplicationStatus.Incompatible,
                            Reason = compatibility.Reasoning
                        };
                        cycleResults.Add(result);
                        sessionResults.Add(result);
                        Log.Information("Skipped (incompatible): {Reason}", compatibility.Reasoning);

                        await historyService.SaveJobResultAsync(ToHistoryEntry(job, result));
                        await searchService.DismissJobAsync(page, job.JobUrl);
                        continue;
                    }

                    // Check Easy Apply
                    if (!job.IsEasyApply)
                    {
                        var result = new ApplicationResult
                        {
                            Job = job,
                            Status = ApplicationStatus.Skipped,
                            Reason = "Not an Easy Apply job"
                        };
                        cycleResults.Add(result);
                        sessionResults.Add(result);

                        await historyService.SaveJobResultAsync(ToHistoryEntry(job, result));
                        continue;
                    }

                    // Navigate to job and apply
                    await page.GotoAsync(job.JobUrl, new PageGotoOptions
                    {
                        WaitUntil = WaitUntilState.DOMContentLoaded,
                        Timeout = 30000
                    });

                    await Task.Delay(Random.Shared.Next(2000, 4000), cts.Token);

                    var success = await applyService.ApplyToJobAsync(page, job, PromptUserToContinue);

                    var appResult = new ApplicationResult
                    {
                        Job = job,
                        Status = success ? ApplicationStatus.Applied : ApplicationStatus.Failed,
                        AppliedAt = success ? DateTime.UtcNow : null,
                        Reason = success ? null : "Application process failed"
                    };
                    cycleResults.Add(appResult);
                    sessionResults.Add(appResult);

                    await historyService.SaveJobResultAsync(ToHistoryEntry(job, appResult));

                    if (success)
                    {
                        appliedCount++;
                        Log.Information("Successfully applied to: {Title} at {Company} ({Count}/{Max})",
                            job.Title, job.Company, appliedCount, settings.MaxApplicationsPerRun);
                    }

                    // Rate limiting between applications
                    await Task.Delay(Random.Shared.Next(3000, 7000), cts.Token);
                }
                catch (OperationCanceledException)
                {
                    throw; // Let it propagate to exit the loop
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error processing job: {Title} at {Company}", job.Title, job.Company);

                    var failResult = new ApplicationResult
                    {
                        Job = job,
                        Status = ApplicationStatus.Failed,
                        Reason = ex.Message
                    };
                    cycleResults.Add(failResult);
                    sessionResults.Add(failResult);

                    await historyService.SaveJobResultAsync(ToHistoryEntry(job, failResult));
                }
            }

            PrintCycleSummary(cycleNumber, cycleResults, allJobs.Count);
        }
        catch (OperationCanceledException)
        {
            Log.Information("Cycle {Cycle} interrupted by shutdown.", cycleNumber);
            break;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error during cycle {Cycle}. Will retry next cycle.", cycleNumber);
        }

        await WaitForNextCycle(settings.SearchIntervalMinutes, cts.Token);
    }

    // 4. Session summary
    PrintSessionSummary(sessionResults, sessionStartedAt, cycleNumber);

    await browserService.DisposeAsync();
    Log.Information("Browser closed. Goodbye!");
}
catch (OperationCanceledException)
{
    PrintSessionSummary(sessionResults, sessionStartedAt, 0);
    Log.Information("Shutdown complete.");
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}

// --- Helper methods ---

static void PrintCycleSummary(int cycleNumber, List<ApplicationResult> results, int totalJobsFound)
{
    Log.Information("");
    Log.Information("--- Cycle {Cycle} Summary ---", cycleNumber);
    Log.Information("Total jobs found:    {Total}", totalJobsFound);
    Log.Information("New jobs processed:  {Processed}", results.Count);
    Log.Information("Applied:             {Applied}", results.Count(r => r.Status == ApplicationStatus.Applied));
    Log.Information("Incompatible:        {Incompatible}", results.Count(r => r.Status == ApplicationStatus.Incompatible));
    Log.Information("Skipped:             {Skipped}", results.Count(r => r.Status == ApplicationStatus.Skipped));
    Log.Information("Failed:              {Failed}", results.Count(r => r.Status == ApplicationStatus.Failed));
    Log.Information("----------------------------");
}

static void PrintSessionSummary(List<ApplicationResult> results, DateTime startedAt, int totalCycles)
{
    var elapsed = DateTime.UtcNow - startedAt;

    Log.Information("");
    Log.Information("========== SESSION SUMMARY ==========");
    Log.Information("Total cycles:        {Cycles}", totalCycles);
    Log.Information("Session duration:    {Duration}", elapsed.ToString(@"hh\:mm\:ss"));
    Log.Information("Total jobs analyzed: {Total}", results.Count);
    Log.Information("Applied:             {Applied}", results.Count(r => r.Status == ApplicationStatus.Applied));
    Log.Information("Incompatible:        {Incompatible}", results.Count(r => r.Status == ApplicationStatus.Incompatible));
    Log.Information("Skipped:             {Skipped}", results.Count(r => r.Status == ApplicationStatus.Skipped));
    Log.Information("Failed:              {Failed}", results.Count(r => r.Status == ApplicationStatus.Failed));
    Log.Information("=====================================");

    var appliedJobs = results.Where(r => r.Status == ApplicationStatus.Applied).ToList();
    if (appliedJobs.Count > 0)
    {
        Log.Information("");
        Log.Information("Applied to:");
        foreach (var r in appliedJobs)
        {
            Log.Information("  - {Title} at {Company}", r.Job.Title, r.Job.Company);
        }
    }
}

static JobHistoryEntry ToHistoryEntry(JobListing job, ApplicationResult result)
{
    return new JobHistoryEntry
    {
        JobUrl = job.JobUrl,
        Title = job.Title,
        Company = job.Company,
        Location = job.Location,
        Status = result.Status.ToString(),
        Reason = result.Reason,
        AnalyzedAt = DateTime.UtcNow,
        AppliedAt = result.AppliedAt
    };
}

static bool PromptUserToContinue(int step)
{
    Log.Warning("Bot paused: unrecognized form element at step {Step}. Waiting for user input...", step);
    Console.WriteLine();
    Console.WriteLine($"==> Unrecognized element in the form (step {step}).");
    Console.WriteLine("    Resolve the issue manually in the browser, then:");
    Console.WriteLine("    [C] Continue processing this job");
    Console.WriteLine("    [S] Stop the bot");
    Console.Write("    Choice: ");

    while (true)
    {
        var input = Console.ReadLine()?.Trim().ToUpperInvariant();
        if (input is "C") return true;
        if (input is "S") return false;
        Console.Write("    Invalid option. Enter C or S: ");
    }
}

static async Task WaitForNextCycle(int intervalMinutes, CancellationToken cancellationToken)
{
    try
    {
        Log.Information("Next cycle in {Minutes} minute(s)...", intervalMinutes);
        await Task.Delay(TimeSpan.FromMinutes(intervalMinutes), cancellationToken);
    }
    catch (OperationCanceledException)
    {
        Log.Information("Wait interrupted by shutdown.");
    }
}
