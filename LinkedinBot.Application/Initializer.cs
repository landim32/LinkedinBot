using LinkedinBot.Domain.Services;
using LinkedinBot.Domain.Services.Interfaces;
using LinkedinBot.DTO.Models;
using LinkedinBot.Infra.Interfaces.Services;
using LinkedinBot.Infra.Services;
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

        // Infrastructure services
        services.AddSingleton<IChatGptService, ChatGptService>();
        services.AddSingleton<IBrowserService, BrowserService>();
        services.AddSingleton<IJobHistoryService, JobHistoryService>();
        services.AddTransient<ILinkedInAuthService, LinkedInAuthService>();
        services.AddTransient<ILinkedInSearchService, LinkedInSearchService>();
        services.AddTransient<ILinkedInApplyService, LinkedInApplyService>();

        // Domain services
        services.AddTransient<IJobAnalyzerService, JobAnalyzerService>();

        return services;
    }
}
