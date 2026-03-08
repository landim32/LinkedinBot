using LinkedinBot.Domain.Services;
using LinkedinBot.Domain.Services.Interfaces;
using LinkedinBot.DTO.Models;
using LinkedinBot.Infra.Interfaces.AppServices;
using LinkedinBot.Infra.Json.Repositories;
using LinkedinBot.Infra.Postgres.Data;
using LinkedinBot.Infra.Postgres.Repositories;
using LinkedinBot.Infra.AppServices;
using LinkedinBot.Infra.Sqlite.Data;
using LinkedinBot.Infra.Sqlite.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LinkedinBot.Application;

public static class Initializer
{
    public static IServiceCollection RegisterServices(
        this IServiceCollection services, IConfiguration configuration)
    {
        // Settings
        services.Configure<LinkedInSettings>(configuration.GetSection(LinkedInSettings.SectionName));
        services.Configure<OpenAISettings>(configuration.GetSection(OpenAISettings.SectionName));
        services.Configure<JobSearchSettings>(configuration.GetSection(JobSearchSettings.SectionName));
        services.Configure<BrowserSettings>(configuration.GetSection(BrowserSettings.SectionName));
        services.Configure<ResumeSettings>(configuration.GetSection(ResumeSettings.SectionName));
        services.Configure<DataConnectionSettings>(configuration.GetSection(DataConnectionSettings.SectionName));

        // Job history repository (config-based selection)
        var jobHistorySection = configuration.GetSection(DataConnectionSettings.SectionName);
        var provider = jobHistorySection.GetValue<string>(nameof(DataConnectionSettings.Provider)) ?? "json";
        var connectionString = jobHistorySection.GetValue<string>(nameof(DataConnectionSettings.ConnectionString)) ?? string.Empty;

        switch (provider.ToLowerInvariant())
        {
            case "postgres":
                if (string.IsNullOrEmpty(connectionString))
                    throw new InvalidOperationException("DataConnection:ConnectionString is required when using Postgres provider.");
                services.AddDbContext<JobHistoryDbContext>(options => options.UseNpgsql(connectionString));
                services.AddScoped<IJobHistoryAppService, PostgresJobHistoryRepository>();
                break;

            case "sqlite":
                if (string.IsNullOrEmpty(connectionString))
                    connectionString = "Data Source=job-history.db";
                services.AddDbContext<SqliteJobHistoryDbContext>(options => options.UseSqlite(connectionString));
                services.AddScoped<IJobHistoryAppService, SqliteJobHistoryRepository>();
                break;

            default: // json
                services.AddSingleton<IJobHistoryAppService, JsonJobHistoryRepository>();
                break;
        }

        // Infrastructure services
        services.AddSingleton<IChatGptService, ChatGptAppService>();
        services.AddSingleton<IBrowserAppService, BrowserAppService>();
        services.AddTransient<ILinkedInAuthAppService, LinkedInAuthAppService>();
        services.AddTransient<ILinkedInSearchAppService, LinkedInSearchAppService>();
        services.AddTransient<ILinkedInApplyAppService, LinkedInApplyAppService>();

        // Domain services
        services.AddTransient<IJobAnalyzerService, JobAnalyzerService>();

        return services;
    }
}
