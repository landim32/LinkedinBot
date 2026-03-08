using LinkedinBot.DTO.Models;
using LinkedinBot.Infra.Interfaces.AppServices;
using LinkedinBot.Infra.Sqlite.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LinkedinBot.Infra.Sqlite.Repositories;

public class SqliteJobHistoryRepository : IJobHistoryAppService
{
    private readonly SqliteJobHistoryDbContext _context;
    private readonly ILogger<SqliteJobHistoryRepository> _logger;

    public SqliteJobHistoryRepository(
        SqliteJobHistoryDbContext context,
        ILogger<SqliteJobHistoryRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task LoadAsync()
    {
        await _context.Database.MigrateAsync();
        _logger.LogInformation("SQLite job history ready.");
    }

    public bool IsJobAnalyzed(string jobUrl) =>
        _context.JobHistory.Any(e => e.JobUrl == jobUrl);

    public async Task SaveJobResultAsync(JobHistoryEntry entry)
    {
        _context.JobHistory.Add(entry);
        await _context.SaveChangesAsync();
        _logger.LogDebug("Saved job history entry for: {Url}", entry.JobUrl);
    }

    public async Task<List<JobHistoryEntry>> GetHistoryAsync() =>
        await _context.JobHistory.OrderByDescending(e => e.AnalyzedAt).ToListAsync();
}
