using System.Text.Json.Serialization;

namespace ImmichTagManager.Models;

public record GeneratedTagPath(
    [property: JsonPropertyName("path")] string[] Path
);

public record GeneratedTagHierarchy(
    [property: JsonPropertyName("tags")] GeneratedTagPath[] Tags
);
