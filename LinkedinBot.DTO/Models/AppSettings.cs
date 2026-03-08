namespace LinkedinBot.DTO.Models;

public class LinkedInSettings
{
    public const string SectionName = "LinkedIn";

    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class OpenAISettings
{
    public const string SectionName = "OpenAI";

    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-4o";
}

public class JobSearchSettings
{
    public const string SectionName = "JobSearch";

    public string Keywords { get; set; } = string.Empty;
    public string GeoId { get; set; } = "106057199";
    public int Distance { get; set; } = 25;
    public string ExperienceLevel { get; set; } = "4";
    public bool EasyApply { get; set; } = true;
    public string RemoteFilter { get; set; } = "2";
    public int MaxApplicationsPerRun { get; set; } = 50;
    public int SearchIntervalMinutes { get; set; } = 5;
    public int SalaryExpectation { get; set; } = 15000;
    public int MaxFormSteps { get; set; } = 20;
    public bool InteractivePrompt { get; set; } = true;
}

public class BrowserSettings
{
    public const string SectionName = "Browser";

    public string Locale { get; set; } = "pt-BR";
    public bool Headless { get; set; } = false;
    public int SlowMo { get; set; } = 500;
    public string UserDataDir { get; set; } = "./user-data";
    public string Channel { get; set; } = "chrome";
}

public class DataConnectionSettings
{
    public const string SectionName = "DataConnection";

    public string Provider { get; set; } = "sqlite";
    public string ConnectionString { get; set; } = "Data Source=job-history.db";
    public string FilePath { get; set; } = string.Empty;
}

public class ResumeSettings
{
    public const string SectionName = "Resume";

    public string MarkdownPath { get; set; } = "./resume.md";
}
