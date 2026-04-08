using System.Net.Http.Json;
using System.Text.Json;
using ImmichTagManager.Models;
using Microsoft.Extensions.Logging;

namespace ImmichTagManager.Services;

public class ImmichAssetService(HttpClient httpClient, IAppSettingsService settings, ILogger<ImmichAssetService> logger) : IImmichAssetService
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
        response.EnsureSuccessStatusCode();
    }

    public async IAsyncEnumerable<ImmichAsset> GetAllAssetsAsync(int pageSize = 50,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        int page = 1;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var body = JsonContent.Create(new { page, size = pageSize, withExif = true });
            var response = await httpClient.SendAsync(
                ImmichRequest(HttpMethod.Post, "api/search/metadata", body), cancellationToken);
            await EnsureSuccessAsync(response, $"POST api/search/metadata (page={page})");

            var result = await response.Content.ReadFromJsonAsync<ImmichAssetSearchResponse>(JsonOptions, cancellationToken);
            var items = result?.Assets.Items;
            if (items is null || items.Length == 0)
                yield break;

            foreach (var asset in items)
                yield return asset;

            if (result!.Assets.NextPage is null)
                yield break;

            page++;
        }
    }

    public async Task<byte[]> GetThumbnailAsync(string assetId)
    {
        var response = await httpClient.SendAsync(
            ImmichRequest(HttpMethod.Get, $"api/assets/{assetId}/thumbnail?size=preview"));
        await EnsureSuccessAsync(response, $"GET api/assets/{assetId}/thumbnail");
        return await response.Content.ReadAsByteArrayAsync();
    }

    public async Task UpdateDescriptionAsync(string assetId, string description)
    {
        var body = JsonContent.Create(new { description });
        var response = await httpClient.SendAsync(
            ImmichRequest(HttpMethod.Put, $"api/assets/{assetId}", body));
        await EnsureSuccessAsync(response, $"PUT api/assets/{assetId}");
    }
}
