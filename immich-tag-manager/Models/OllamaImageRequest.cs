using System.Text.Json.Serialization;

namespace ImmichTagManager.Models;

public record OllamaImageRequest(
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("prompt")] string Prompt,
    [property: JsonPropertyName("images")] string[] Images,
    [property: JsonPropertyName("stream")] bool Stream,
    [property: JsonPropertyName("format")] object Format,
    [property: JsonPropertyName("options")] object? Options = null
);
