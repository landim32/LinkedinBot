using LinkedinBot.DTO.Models;
using LinkedinBot.Infra.Interfaces.AppServices;
using LinkedinBot.Infra.Postgres.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LinkedinBot.Infra.Postgres.Repositories;

public class PostgresJobHistoryRepository : IJobHistoryAppService
{
    private readonly JobHistoryDbContext _context;
    private readonly ILogger<PostgresJobHistoryRepository> _logger;

    public PostgresJobHistoryRepository(
        JobHistoryDbContext context,
        ILogger<PostgresJobHistoryRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task LoadAsync()
    {
        await _context.Database.MigrateAsync();
        _logger.LogInformation("Postgres job history ready.");
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
