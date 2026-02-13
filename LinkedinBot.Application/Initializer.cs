using LinkedinBot.Domain.Services;
using LinkedinBot.Domain.Services.Interfaces;
using LinkedinBot.DTO.Models;
using LinkedinBot.Infra.Data;
using LinkedinBot.Infra.Interfaces.Services;
using LinkedinBot.Infra.Repositories;
using LinkedinBot.Infra.Services;
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

        // Job history repository (config-based selection)
        var provider = configuration.GetValue<string>("JobHistory:Provider") ?? "json";
        if (provider.Equals("postgres", StringComparison.OrdinalIgnoreCase))
        {
            var connectionString = configuration.GetConnectionString("JobHistory")
                ?? throw new InvalidOperationException(
                    "ConnectionStrings:JobHistory is required when using Postgres provider.");
            services.AddDbContext<JobHistoryDbContext>(options => options.UseNpgsql(connectionString));
            services.AddScoped<IJobHistoryService, PostgresJobHistoryRepository>();
        }
        else
        {
            services.AddSingleton<IJobHistoryService, JsonJobHistoryRepository>();
        }

        // Infrastructure services
        services.AddSingleton<IChatGptService, ChatGptService>();
        services.AddSingleton<IBrowserService, BrowserService>();
        services.AddTransient<ILinkedInAuthService, LinkedInAuthService>();
        services.AddTransient<ILinkedInSearchService, LinkedInSearchService>();
        services.AddTransient<ILinkedInApplyService, LinkedInApplyService>();

        // Domain services
        services.AddTransient<IJobAnalyzerService, JobAnalyzerService>();

        return services;
    }
}
