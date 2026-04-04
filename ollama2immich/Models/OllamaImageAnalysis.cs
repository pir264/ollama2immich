using System.Text.Json.Serialization;

namespace ollama2immich.Models;

public record OllamaImageAnalysis(
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("tags")] string[] Tags
);
