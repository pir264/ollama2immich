using System.Text.Json.Serialization;

namespace ImmichTagManager.Models;

public record OllamaImageAnalysis(
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("tags")] string[] Tags
);
