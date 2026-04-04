using System.Net.Http.Json;
using System.Text.Json;
using ollama2immich.Models;

namespace ollama2immich.Services;

public class ImmichService(HttpClient httpClient)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async IAsyncEnumerable<ImmichAsset> GetAllAssetsAsync(int pageSize = 50)
    {
        int page = 1;
        while (true)
        {
            var response = await httpClient.GetAsync($"api/assets?page={page}&size={pageSize}");
            response.EnsureSuccessStatusCode();

            var assets = await response.Content.ReadFromJsonAsync<ImmichAsset[]>(JsonOptions);
            if (assets is null || assets.Length == 0)
                yield break;

            foreach (var asset in assets)
                yield return asset;

            if (assets.Length < pageSize)
                yield break;

            page++;
        }
    }

    public async Task<byte[]> GetThumbnailAsync(string assetId)
    {
        var response = await httpClient.GetAsync($"api/assets/{assetId}/thumbnail?size=preview");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync();
    }

    public async Task UpdateDescriptionAsync(string assetId, string description)
    {
        var body = JsonContent.Create(new { description });
        var response = await httpClient.PutAsync($"api/assets/{assetId}", body);
        response.EnsureSuccessStatusCode();
    }

    public async Task<List<ImmichTag>> GetTagsAsync()
    {
        var response = await httpClient.GetAsync("api/tags");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<ImmichTag>>(JsonOptions) ?? [];
    }

    public async Task<ImmichTag> CreateTagAsync(string name)
    {
        var body = JsonContent.Create(new { name });
        var response = await httpClient.PostAsync("api/tags", body);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ImmichTag>(JsonOptions))!;
    }

    public async Task AssignTagToAssetAsync(string tagId, string assetId)
    {
        var body = JsonContent.Create(new { ids = new[] { assetId } });
        var response = await httpClient.PutAsync($"api/tags/{tagId}/assets", body);
        response.EnsureSuccessStatusCode();
    }
}
