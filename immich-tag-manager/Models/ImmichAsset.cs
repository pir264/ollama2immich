using System.Text.Json.Serialization;

namespace ImmichTagManager.Models;

public record ImmichAsset(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("exifInfo")] ExifInfo? ExifInfo
);

public record ExifInfo(
    [property: JsonPropertyName("description")] string? Description
);

public record ImmichAssetSearchPage(
    [property: JsonPropertyName("items")] ImmichAsset[] Items,
    [property: JsonPropertyName("nextPage")] string? NextPage
);

public record ImmichAssetSearchResponse(
    [property: JsonPropertyName("assets")] ImmichAssetSearchPage Assets
);
