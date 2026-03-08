using LinkedinBot.DTO.Models;
using Microsoft.EntityFrameworkCore;

namespace LinkedinBot.Infra.Sqlite.Data;

public class SqliteJobHistoryDbContext : DbContext
{
    public DbSet<JobHistoryEntry> JobHistory { get; set; } = null!;

    public SqliteJobHistoryDbContext(DbContextOptions<SqliteJobHistoryDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<JobHistoryEntry>(entity =>
        {
            entity.ToTable("job_history");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.JobUrl).IsRequired();
            entity.Property(e => e.Title).IsRequired();
            entity.Property(e => e.Company).IsRequired();
            entity.Property(e => e.Location).IsRequired();
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.AnalyzedAt).IsRequired();

            entity.HasIndex(e => e.JobUrl).IsUnique();
        });
    }
}
