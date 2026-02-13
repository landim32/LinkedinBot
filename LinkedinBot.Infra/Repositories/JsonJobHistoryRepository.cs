using System.Text.Json;
using LinkedinBot.DTO.Models;
using LinkedinBot.Infra.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LinkedinBot.Infra.Repositories;

public class JsonJobHistoryRepository : IJobHistoryService
{
    private readonly string _filePath;
    private readonly ILogger<JsonJobHistoryRepository> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly HashSet<string> _analyzedUrls = new(StringComparer.OrdinalIgnoreCase);
    private List<JobHistoryEntry> _history = [];

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public JsonJobHistoryRepository(IOptions<JobSearchSettings> settings, ILogger<JsonJobHistoryRepository> logger)
    {
        _filePath = Path.GetFullPath(settings.Value.HistoryFilePath);
        _logger = logger;
    }

    public async Task LoadAsync()
    {
        if (!File.Exists(_filePath))
        {
            _logger.LogInformation("No history file found at {Path}. Starting fresh.", _filePath);
            return;
        }

        try
        {
            var json = await File.ReadAllTextAsync(_filePath);
            _history = JsonSerializer.Deserialize<List<JobHistoryEntry>>(json) ?? [];

            foreach (var entry in _history)
            {
                _analyzedUrls.Add(entry.JobUrl);
            }

            _logger.LogInformation("Loaded {Count} entries from job history at {Path}.", _history.Count, _filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load history file at {Path}. Starting fresh.", _filePath);
            _history = [];
            _analyzedUrls.Clear();
        }
    }

    public bool IsJobAnalyzed(string jobUrl) => _analyzedUrls.Contains(jobUrl);

    public async Task SaveJobResultAsync(JobHistoryEntry entry)
    {
        await _lock.WaitAsync();
        try
        {
            _analyzedUrls.Add(entry.JobUrl);
            _history.Add(entry);
            await PersistAsync();
        }
        finally
        {
            _lock.Release();
        }
    }

    public Task<List<JobHistoryEntry>> GetHistoryAsync() =>
        Task.FromResult(_history.ToList());

    private async Task PersistAsync()
    {
        var json = JsonSerializer.Serialize(_history, JsonOptions);
        await File.WriteAllTextAsync(_filePath, json);
    }
}
