using System.Text.Json.Serialization;

namespace ollama2immich.Models;

public record OllamaResponse(
    [property: JsonPropertyName("response")] string Response
);
