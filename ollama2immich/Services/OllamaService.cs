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

    public async Task<(string Description, string[] Tags)> AnalyzeImageAsync(byte[] imageBytes)
    {
        var base64 = Convert.ToBase64String(imageBytes);
        var request = new OllamaRequest(model, prompt, [base64], Stream: false);

        var response = await httpClient.PostAsJsonAsync("api/generate", request);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<OllamaResponse>(JsonOptions);
        if (result is null)
            throw new InvalidOperationException("Empty response from Ollama.");

        return ParseResponse(result.Response);
    }

    private static (string Description, string[] Tags) ParseResponse(string raw)
    {
        var description = string.Empty;
        var tags = Array.Empty<string>();

        var lines = raw.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            if (line.StartsWith("DESCRIPTION:", StringComparison.OrdinalIgnoreCase))
                description = line["DESCRIPTION:".Length..].Trim();
            else if (line.StartsWith("TAGS:", StringComparison.OrdinalIgnoreCase))
                tags = line["TAGS:".Length..]
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .ToArray();
        }

        if (string.IsNullOrWhiteSpace(description))
            throw new FormatException($"Could not parse DESCRIPTION from Ollama response: {raw}");

        return (description, tags);
    }
}
