using System.Net.Http.Json;
using System.Text.Json;
using ImmichTagManager.Models;
using Microsoft.Extensions.Logging;

namespace ImmichTagManager.Services;

public class OllamaTagGeneratorService(HttpClient httpClient, string model, string promptTemplate, ILogger<OllamaTagGeneratorService> logger) : IOllamaTagGeneratorService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<List<string[]>> GenerateTagHierarchyAsync(int maxTags, int depth)
    {
        var prompt = string.Format(promptTemplate, maxTags, depth);
        var schema = BuildSchema(maxTags);
        var request = new OllamaTextRequest(model, prompt, false, schema, new { temperature = 0 });

        var response = await httpClient.PostAsJsonAsync("api/generate", request);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            logger.LogError("Ollama api/generate failed: {StatusCode} {ReasonPhrase} — {Body}",
                (int)response.StatusCode, response.ReasonPhrase, body);
            response.EnsureSuccessStatusCode();
        }

        var ollamaResponse = await response.Content.ReadFromJsonAsync<OllamaTextResponse>(JsonOptions)
            ?? throw new InvalidOperationException("Lege response van Ollama.");

        var result = JsonSerializer.Deserialize<GeneratedTagHierarchy>(ollamaResponse.Response, JsonOptions)
            ?? throw new InvalidOperationException("Kon Ollama response niet deserialiseren.");

        return result.Tags
            .Where(t => t.Path is { Length: > 0 })
            .Select(t => t.Path)
            .ToList();
    }

    private static object BuildSchema(int maxTags) => new
    {
        type = "object",
        properties = new
        {
            tags = new
            {
                type = "array",
                maxItems = maxTags,
                items = new
                {
                    type = "object",
                    properties = new
                    {
                        path = new
                        {
                            type = "array",
                            items = new { type = "string" }
                        }
                    },
                    required = new[] { "path" }
                }
            }
        },
        required = new[] { "tags" }
    };
}
