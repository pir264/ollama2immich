using System.Text.Json.Serialization;

namespace ImmichTagManager.Models;

public record ImmichTag(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("parentId")] string? ParentId
);
