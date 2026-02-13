using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LinkedinBot.Infra.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<JobHistoryDbContext>
{
    public JobHistoryDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<JobHistoryDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Database=linkedinbot;Username=linkedinbot;Password=linkedinbot");
        return new JobHistoryDbContext(optionsBuilder.Options);
    }
}
