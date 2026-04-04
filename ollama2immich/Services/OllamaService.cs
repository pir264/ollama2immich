using System.Net.Http.Json;
using System.Text.Json;
using ollama2immich.Models;

namespace ollama2immich.Services;

public class OllamaService(HttpClient httpClient, string model, string prompt)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly object Schema = new
    {
        type = "object",
        properties = new
        {
            description = new { type = "string" },
            tags = new { type = "array", items = new { type = "string" } }
        },
        required = new[] { "description", "tags" }
    };

    private static readonly object Options = new { temperature = 0 };

    public async Task<(string Description, string[] Tags)> AnalyzeImageAsync(byte[] imageBytes)
    {
        var base64 = Convert.ToBase64String(imageBytes);
        var request = new OllamaRequest(model, prompt, [base64], Stream: false, Format: Schema, Options: Options);

        var response = await httpClient.PostAsJsonAsync("api/generate", request);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<OllamaResponse>(JsonOptions);
        if (result is null)
            throw new InvalidOperationException("Empty response from Ollama.");

        var analysis = JsonSerializer.Deserialize<OllamaImageAnalysis>(result.Response, JsonOptions);
        if (analysis is null || string.IsNullOrWhiteSpace(analysis.Description))
            throw new InvalidOperationException($"Could not deserialize structured response: {result.Response}");

        return (analysis.Description, analysis.Tags ?? []);
    }
}
