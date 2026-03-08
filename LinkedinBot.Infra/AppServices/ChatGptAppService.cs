using System.Text.Json;
using LinkedinBot.Domain.Services.Interfaces;
using LinkedinBot.DTO.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Chat;

namespace LinkedinBot.Infra.AppServices;

public class ChatGptAppService : IChatGptService
{
    private readonly ChatClient _client;
    private readonly string _resumeContent;
    private readonly int _salaryExpectation;
    private readonly ILogger<ChatGptAppService> _logger;

    public ChatGptAppService(
        IOptions<OpenAISettings> openAiSettings,
        IOptions<ResumeSettings> resumeSettings,
        IOptions<JobSearchSettings> jobSearchSettings,
        ILogger<ChatGptAppService> logger)
    {
        _logger = logger;
        _salaryExpectation = jobSearchSettings.Value.SalaryExpectation;
        var settings = openAiSettings.Value;

        _client = new ChatClient(model: settings.Model, apiKey: settings.ApiKey);

        var resumePath = Path.GetFullPath(resumeSettings.Value.MarkdownPath);
        _resumeContent = File.ReadAllText(resumePath);

        _logger.LogInformation("ChatGPT service initialized with model {Model}", settings.Model);
    }

    public async Task<CompatibilityResult> AnalyzeJobCompatibilityAsync(JobListing job)
    {
        _logger.LogDebug("Analyzing compatibility for: {Title} at {Company}", job.Title, job.Company);

        var systemPrompt = """
            You are a job compatibility analyzer. Given a candidate's resume and a job listing,
            determine if the candidate is a good fit for the position.

            Consider:
            - Required skills vs candidate's skills
            - Experience level match
            - Technology stack alignment
            - The job should be related to .NET, C#, or backend development
            - Reject jobs that are primarily Java, Python, or other non-.NET stacks

            Respond ONLY with valid JSON matching this exact schema (no markdown, no extra text):
            {
              "isCompatible": true/false,
              "confidenceScore": 0.0 to 1.0,
              "reasoning": "brief explanation",
              "keyMatchingSkills": ["skill1", "skill2"],
              "missingRequirements": ["req1", "req2"]
            }
            """;

        var userPrompt = $"""
            ## Candidate Resume:
            {_resumeContent}

            ## Job Listing:
            Title: {job.Title}
            Company: {job.Company}
            Location: {job.Location}

            Description:
            {TruncateText(job.Description, 4000)}
            """;

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(systemPrompt),
            new UserChatMessage(userPrompt)
        };

        var completion = await _client.CompleteChatAsync(messages);
        var responseText = completion.Value.Content[0].Text.Trim();

        responseText = CleanJsonResponse(responseText);

        try
        {
            var result = JsonSerializer.Deserialize<CompatibilityResult>(responseText);
            return result ?? new CompatibilityResult { IsCompatible = false, Reasoning = "Failed to parse response" };
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse compatibility response: {Response}", responseText);
            return new CompatibilityResult
            {
                IsCompatible = false,
                Reasoning = $"Parse error: {ex.Message}"
            };
        }
    }

    public async Task<string> AnswerFormQuestionAsync(string question, List<string>? options = null)
    {
        _logger.LogDebug("Answering form question: {Question}", question);

        var optionsText = options is { Count: > 0 }
            ? $"\n\nAvailable options (pick the best one exactly as written):\n- {string.Join("\n- ", options)}"
            : "";

        var systemPrompt = $"""
            You are helping fill out a job application form. Based on the candidate's resume,
            answer the question concisely and accurately.

            Rules:
            - If options are provided, reply with EXACTLY one of the provided options (copy it verbatim)
            - If it's a text field, give a brief, professional answer
            - If asked about years of experience with a technology, give a realistic number based on the resume
            - If asked about salary expectations, reply with ONLY the integer {_salaryExpectation} (no text, no currency, no dots, no commas — just the number)
            - If asked about availability/start date, say "Imediata" (immediate)
            - For yes/no questions, answer "Sim" (yes) unless the resume clearly indicates otherwise
            - If the field expects a number (numeric input), reply with ONLY a plain integer number — never include text, symbols, currency signs, or letters
            - If asked for a phone number or "celular", reply with ONLY digits (e.g. 61998752588) — no parentheses, no dashes, no spaces
            - Reply with ONLY the answer, no explanations or extra text
            """;

        var userPrompt = $"""
            ## Candidate Resume:
            {_resumeContent}

            ## Question: {question}{optionsText}
            """;

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(systemPrompt),
            new UserChatMessage(userPrompt)
        };

        var completion = await _client.CompleteChatAsync(messages);
        var answer = completion.Value.Content[0].Text.Trim();

        _logger.LogInformation("Q: {Question} → A: {Answer}", question, answer);
        return answer;
    }

    private static string TruncateText(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            return text;

        return text[..maxLength] + "\n[...truncated]";
    }

    private static string CleanJsonResponse(string response)
    {
        if (response.StartsWith("```"))
        {
            var lines = response.Split('\n');
            var jsonLines = lines
                .SkipWhile(l => l.StartsWith("```"))
                .TakeWhile(l => !l.StartsWith("```"))
                .ToArray();
            return string.Join('\n', jsonLines);
        }
        return response;
    }
}
