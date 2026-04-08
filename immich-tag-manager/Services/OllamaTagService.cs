using System.Net.Http.Json;
using System.Text.Json;
using ImmichTagManager.Models;
using Microsoft.Extensions.Logging;

namespace ImmichTagManager.Services;

public class OllamaTagService(HttpClient httpClient, IAppSettingsService settings, ILogger<OllamaTagService> logger) : IOllamaTagService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private static readonly object Schema = new
    {
        type = "object",
        properties = new
        {
            renames = new
            {
                type = "array",
                items = new
                {
                    type = "object",
                    properties = new
                    {
                        from = new { type = "string" },
                        to = new { type = "string" }
                    },
                    required = new[] { "from", "to" }
                }
            },
            merges = new
            {
                type = "array",
                items = new
                {
                    type = "object",
                    properties = new
                    {
                        keep = new { type = "string" },
                        discard = new { type = "array", items = new { type = "string" } }
                    },
                    required = new[] { "keep", "discard" }
                }
            },
            parents = new
            {
                type = "array",
                items = new
                {
                    type = "object",
                    properties = new
                    {
                        parent = new { type = "string" },
                        children = new { type = "array", items = new { type = "string" } },
                        is_new = new { type = "boolean" }
                    },
                    required = new[] { "parent", "children", "is_new" }
                }
            }
        },
        required = new[] { "renames", "merges", "parents" }
    };

    public async Task<OllamaTagAnalysis> AnalyzeTagsAsync(IList<ImmichTag> tags)
    {
        var s = settings.GetSettings();
        var tagList = string.Join("\n", tags.Select(t => $"- {t.Name}"));
        var fullPrompt = $"{s.TagPrompt}\n\nTags:\n{tagList}";

        var request = new OllamaTextRequest(s.OllamaModel, fullPrompt, false, Schema, new { temperature = 0 });
        var response = await httpClient.PostAsJsonAsync(s.OllamaBaseUrl.TrimEnd('/') + "/api/generate", request);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            logger.LogError("Ollama api/generate failed: {StatusCode} {ReasonPhrase} — {Body}",
                (int)response.StatusCode, response.ReasonPhrase, body);
            response.EnsureSuccessStatusCode();
        }

        var ollamaResponse = await response.Content.ReadFromJsonAsync<OllamaTextResponse>(JsonOptions)
            ?? throw new InvalidOperationException("Lege response van Ollama.");

        return JsonSerializer.Deserialize<OllamaTagAnalysis>(ollamaResponse.Response, JsonOptions)
            ?? throw new InvalidOperationException("Kon Ollama response niet deserialiseren.");
    }
}
