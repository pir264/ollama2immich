using System.Net.Http.Json;
using System.Text.Json;
using ImmichTagManager.Models;
using Microsoft.Extensions.Logging;

namespace ImmichTagManager.Services;

public class OllamaImageService(
    HttpClient httpClient,
    string model,
    string prompt,
    string tagExistingPrompt,
    ILogger<OllamaImageService> logger)
    : IOllamaImageService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private static readonly object AnalyzeSchema = new
    {
        type = "object",
        properties = new
        {
            description = new { type = "string" },
            tags = new { type = "array", items = new { type = "string" } }
        },
        required = new[] { "description", "tags" }
    };

    private static readonly object SelectSchema = new
    {
        type = "object",
        properties = new
        {
            tags = new { type = "array", items = new { type = "string" } }
        },
        required = new[] { "tags" }
    };

    private static readonly object Options = new { temperature = 0 };

    public async Task<(string Description, string[] Tags)> AnalyzeImageAsync(byte[] imageBytes)
    {
        var base64 = Convert.ToBase64String(imageBytes);
        var request = new OllamaImageRequest(model, prompt, [base64], Stream: false, Format: AnalyzeSchema, Options: Options);

        var response = await httpClient.PostAsJsonAsync("api/generate", request);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<OllamaImageResponse>(JsonOptions);
        if (result is null)
            throw new InvalidOperationException("Empty response from Ollama.");

        var analysis = JsonSerializer.Deserialize<OllamaImageAnalysis>(result.Response, JsonOptions);
        if (analysis is null || string.IsNullOrWhiteSpace(analysis.Description))
            throw new InvalidOperationException($"Could not deserialize structured response: {result.Response}");

        logger.LogDebug("Ollama analyzed image: {Description}, tags: {Tags}",
            analysis.Description, string.Join(", ", analysis.Tags ?? []));

        return (analysis.Description, analysis.Tags ?? []);
    }

    public async Task<string[]> SelectTagsAsync(byte[] imageBytes, string[] tagNames)
    {
        var tagList = string.Join("\n", tagNames.Select(t => $"- {t}"));
        var fullPrompt = $"{tagExistingPrompt}\n\nBeschikbare tags:\n{tagList}";
        var base64 = Convert.ToBase64String(imageBytes);
        var request = new OllamaImageRequest(model, fullPrompt, [base64], Stream: false, Format: SelectSchema, Options: Options);

        var response = await httpClient.PostAsJsonAsync("api/generate", request);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<OllamaImageResponse>(JsonOptions);
        if (result is null)
            throw new InvalidOperationException("Empty response from Ollama.");

        var analysis = JsonSerializer.Deserialize<OllamaImageAnalysis>(result.Response, JsonOptions);
        return analysis?.Tags ?? [];
    }
}
