using System.Text.Json.Serialization;

namespace ImmichTagManager.Models;

public record OllamaTextResponse(
    [property: JsonPropertyName("response")] string Response
);
