using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace ImmichTagManager.Services;

public class OllamaModelsService(HttpClient httpClient) : IOllamaModelsService
{
    private record OllamaTagsResponse(
        [property: JsonPropertyName("models")] List<OllamaModelEntry> Models);

    private record OllamaModelEntry(
        [property: JsonPropertyName("name")] string Name);

    public async Task<List<string>> GetAvailableModelsAsync(string ollamaBaseUrl)
    {
        var url = ollamaBaseUrl.TrimEnd('/') + "/api/tags";
        try
        {
            var response = await httpClient.GetFromJsonAsync<OllamaTagsResponse>(url);
            return response?.Models.Select(m => m.Name).OrderBy(n => n).ToList() ?? [];
        }
        catch
        {
            return [];
        }
    }
}
