using System.Text.Json.Serialization;

namespace ollama2immich.Models;

public record ImmichAsset(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("exifInfo")] ExifInfo? ExifInfo
);

public record ExifInfo(
    [property: JsonPropertyName("description")] string? Description
);
