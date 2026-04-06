using System.Text.Json.Serialization;

namespace ImmichTagManager.Models;

public record OllamaTextRequest(
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("prompt")] string Prompt,
    [property: JsonPropertyName("stream")] bool Stream,
    [property: JsonPropertyName("format")] object Format,
    [property: JsonPropertyName("options")] object? Options = null
);
