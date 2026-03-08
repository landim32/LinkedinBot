using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LinkedinBot.Infra.Sqlite.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<SqliteJobHistoryDbContext>
{
    public SqliteJobHistoryDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SqliteJobHistoryDbContext>();
        optionsBuilder.UseSqlite("Data Source=job-history.db");
        return new SqliteJobHistoryDbContext(optionsBuilder.Options);
    }
}
