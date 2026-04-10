using System.Net.Http.Json;
using System.Text.Json;
using ImmichTagManager.Models;
using Microsoft.Extensions.Logging;

namespace ImmichTagManager.Services;

public class OllamaImageService(
    HttpClient httpClient,
    IAppSettingsService settings,
    ILogger<OllamaImageService> logger,
    string? baseUrlOverride = null,
    string? modelOverride = null)
    : IOllamaImageService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<bool> IsModelAvailableAsync()
    {
        var s = settings.GetSettings();
        var baseUrl = (baseUrlOverride ?? s.OllamaBaseUrl).TrimEnd('/');
        var model = modelOverride ?? s.OllamaImageModel;
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var response = await httpClient.GetAsync(baseUrl + "/api/tags", cts.Token);
            if (!response.IsSuccessStatusCode) return false;
            var body = await response.Content.ReadAsStringAsync(cts.Token);
            using var doc = JsonDocument.Parse(body);
            if (!doc.RootElement.TryGetProperty("models", out var modelsEl)) return false;
            foreach (var m in modelsEl.EnumerateArray())
            {
                if (!m.TryGetProperty("name", out var nameEl)) continue;
                var name = nameEl.GetString() ?? "";
                if (name.Equals(model, StringComparison.OrdinalIgnoreCase) ||
                    name.StartsWith(model + ":", StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            logger.LogDebug("Ollama beschikbaarheidscheck mislukt voor {BaseUrl} ({Model}): {Message}", baseUrl, model, ex.Message);
            return false;
        }
    }

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
        var s = settings.GetSettings();
        var baseUrl = (baseUrlOverride ?? s.OllamaBaseUrl).TrimEnd('/');
        var model   = modelOverride ?? s.OllamaImageModel;
        var base64 = Convert.ToBase64String(imageBytes);
        var request = new OllamaImageRequest(model, s.ImagePrompt, [base64], Stream: false, Format: AnalyzeSchema, Options: Options);

        var response = await httpClient.PostAsJsonAsync(baseUrl + "/api/generate", request);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(body))
            throw new InvalidOperationException($"Ollama ({model} @ {baseUrl}) returned an empty response body.");

        var result = JsonSerializer.Deserialize<OllamaImageResponse>(body, JsonOptions);
        if (result is null)
            throw new InvalidOperationException($"Could not deserialize Ollama response: {body}");

        var analysis = JsonSerializer.Deserialize<OllamaImageAnalysis>(result.Response, JsonOptions);
        if (analysis is null || string.IsNullOrWhiteSpace(analysis.Description))
            throw new InvalidOperationException($"Could not deserialize structured response: {result.Response}");

        logger.LogDebug("Ollama analyzed image: {Description}, tags: {Tags}",
            analysis.Description, string.Join(", ", analysis.Tags ?? []));

        return (analysis.Description, analysis.Tags ?? []);
    }

    public async Task<string[]> SelectTagsAsync(byte[] imageBytes, string[] tagNames)
    {
        var s = settings.GetSettings();
        var baseUrl = (baseUrlOverride ?? s.OllamaBaseUrl).TrimEnd('/');
        var model   = modelOverride ?? s.OllamaImageModel;
        var tagList = string.Join("\n", tagNames.Select(t => $"- {t}"));
        var fullPrompt = $"{s.TagExistingPrompt}\n\nBeschikbare tags:\n{tagList}";
        var base64 = Convert.ToBase64String(imageBytes);
        var request = new OllamaImageRequest(model, fullPrompt, [base64], Stream: false, Format: SelectSchema, Options: Options);

        var response = await httpClient.PostAsJsonAsync(baseUrl + "/api/generate", request);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(body))
            throw new InvalidOperationException($"Ollama ({model} @ {baseUrl}) returned an empty response body.");

        var result = JsonSerializer.Deserialize<OllamaImageResponse>(body, JsonOptions);
        if (result is null)
            throw new InvalidOperationException($"Could not deserialize Ollama response: {body}");

        var analysis = JsonSerializer.Deserialize<OllamaImageAnalysis>(result.Response, JsonOptions);
        return analysis?.Tags ?? [];
    }
}
