using System.Text.Json.Serialization;

namespace ImmichTagManager.Models;

public record ImmichSearchAsset(
    [property: JsonPropertyName("id")] string Id
);

public record ImmichSearchPage(
    [property: JsonPropertyName("items")] ImmichSearchAsset[] Items,
    [property: JsonPropertyName("nextPage")] string? NextPage
);

public record ImmichSearchResponse(
    [property: JsonPropertyName("assets")] ImmichSearchPage Assets
);
