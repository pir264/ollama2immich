using System.Text.Json.Serialization;

namespace ollama2immich.Models;

public record ImmichSearchResponse(
    [property: JsonPropertyName("assets")] ImmichSearchAssets Assets
);

public record ImmichSearchAssets(
    [property: JsonPropertyName("items")] ImmichAsset[] Items,
    [property: JsonPropertyName("nextPage")] string? NextPage
);
