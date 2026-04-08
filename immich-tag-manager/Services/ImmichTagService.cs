using System.Net.Http.Json;
using System.Text.Json;
using ImmichTagManager.Models;
using Microsoft.Extensions.Logging;

namespace ImmichTagManager.Services;

public class ImmichTagService(HttpClient httpClient, IAppSettingsService settings, ILogger<ImmichTagService> logger) : IImmichTagService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private HttpRequestMessage ImmichRequest(HttpMethod method, string path, HttpContent? content = null)
    {
        var s = settings.GetSettings();
        var req = new HttpRequestMessage(method, s.ImmichBaseUrl.TrimEnd('/') + "/" + path.TrimStart('/'));
        req.Headers.Add("x-api-key", s.ImmichApiKey);
        req.Content = content;
        return req;
    }

    private async Task EnsureSuccessAsync(HttpResponseMessage response, string operation)
    {
        if (response.IsSuccessStatusCode) return;
        var body = await response.Content.ReadAsStringAsync();
        logger.LogError("Immich {Operation} failed: {StatusCode} {ReasonPhrase} — {Body}",
            operation, (int)response.StatusCode, response.ReasonPhrase, body);
        throw new HttpRequestException(
            $"{operation}: {(int)response.StatusCode} {response.ReasonPhrase} — {body}",
            null, response.StatusCode);
    }

    public async Task<List<ImmichTag>> GetTagsAsync()
    {
        var response = await httpClient.SendAsync(ImmichRequest(HttpMethod.Get, "api/tags"));
        await EnsureSuccessAsync(response, "GET api/tags");
        return await response.Content.ReadFromJsonAsync<List<ImmichTag>>(JsonOptions) ?? [];
    }

    public async Task<ImmichTag> CreateTagAsync(string name, string? parentId = null)
    {
        var body = parentId is not null
            ? JsonContent.Create(new { name, parentId })
            : JsonContent.Create(new { name });
        var response = await httpClient.SendAsync(ImmichRequest(HttpMethod.Post, "api/tags", body));
        await EnsureSuccessAsync(response, $"POST api/tags ({name})");
        return (await response.Content.ReadFromJsonAsync<ImmichTag>(JsonOptions))!;
    }

    public async Task UpdateTagAsync(string tagId, string? name = null, string? parentId = null)
    {
        var payload = new Dictionary<string, object?>();
        if (name is not null) payload["name"] = name;
        if (parentId is not null) payload["parentId"] = parentId;

        var body = JsonContent.Create(payload);
        var response = await httpClient.SendAsync(ImmichRequest(HttpMethod.Patch, $"api/tags/{tagId}", body));
        await EnsureSuccessAsync(response, $"PATCH api/tags/{tagId} (name={name}, parentId={parentId})");
    }

    public async Task DeleteTagAsync(string tagId)
    {
        var response = await httpClient.SendAsync(ImmichRequest(HttpMethod.Delete, $"api/tags/{tagId}"));
        await EnsureSuccessAsync(response, $"DELETE api/tags/{tagId}");
    }

    public async Task<List<string>> GetAssetIdsByTagAsync(string tagId)
    {
        var assetIds = new List<string>();
        int page = 1;
        while (true)
        {
            var body = JsonContent.Create(new { page, size = 100, tagIds = new[] { tagId } });
            var response = await httpClient.SendAsync(ImmichRequest(HttpMethod.Post, "api/search/metadata", body));
            await EnsureSuccessAsync(response, $"POST api/search/metadata (tagId={tagId}, page={page})");

            var result = await response.Content.ReadFromJsonAsync<ImmichSearchResponse>(JsonOptions);
            var items = result?.Assets.Items;
            if (items is null || items.Length == 0)
                break;

            assetIds.AddRange(items.Select(i => i.Id));

            if (result!.Assets.NextPage is null)
                break;

            page++;
        }
        return assetIds;
    }

    public async Task AssignTagToAssetsAsync(string tagId, IList<string> assetIds)
    {
        var body = JsonContent.Create(new { ids = assetIds });
        var response = await httpClient.SendAsync(ImmichRequest(HttpMethod.Put, $"api/tags/{tagId}/assets", body));
        await EnsureSuccessAsync(response, $"PUT api/tags/{tagId}/assets ({assetIds.Count} assets)");
    }
}
