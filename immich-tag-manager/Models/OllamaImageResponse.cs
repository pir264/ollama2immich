using System.Text.Json.Serialization;

namespace ImmichTagManager.Models;

public record OllamaImageResponse(
    [property: JsonPropertyName("response")] string Response
);
