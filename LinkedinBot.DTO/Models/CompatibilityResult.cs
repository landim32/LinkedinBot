using System.Text.Json.Serialization;

namespace LinkedinBot.DTO.Models;

public class CompatibilityResult
{
    [JsonPropertyName("isCompatible")]
    public bool IsCompatible { get; set; }

    [JsonPropertyName("confidenceScore")]
    public double ConfidenceScore { get; set; }

    [JsonPropertyName("reasoning")]
    public string Reasoning { get; set; } = string.Empty;

    [JsonPropertyName("keyMatchingSkills")]
    public List<string> KeyMatchingSkills { get; set; } = [];

    [JsonPropertyName("missingRequirements")]
    public List<string> MissingRequirements { get; set; } = [];
}
