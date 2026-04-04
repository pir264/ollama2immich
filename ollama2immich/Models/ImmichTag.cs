using System.Text.Json.Serialization;

namespace ollama2immich.Models;

public record ImmichTag(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name
);
