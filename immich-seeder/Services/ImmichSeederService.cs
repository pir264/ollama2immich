using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ImmichSeeder.Models;

namespace ImmichSeeder.Services;

public class ImmichSeederService(HttpClient httpClient)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<string> GetOrCreateAlbumAsync(string name)
    {
        var response = await httpClient.GetAsync("api/albums");
        response.EnsureSuccessStatusCode();

        var albums = await JsonSerializer.DeserializeAsync<ImmichAlbum[]>(
            await response.Content.ReadAsStreamAsync(), JsonOptions);

        var existing = albums?.FirstOrDefault(a =>
            string.Equals(a.AlbumName, name, StringComparison.OrdinalIgnoreCase));

        if (existing is not null) return existing.Id;

        var createResponse = await httpClient.PostAsJsonAsync("api/albums", new { albumName = name });
        createResponse.EnsureSuccessStatusCode();

        var created = await JsonSerializer.DeserializeAsync<ImmichAlbum>(
            await createResponse.Content.ReadAsStreamAsync(), JsonOptions);

        return created!.Id;
    }

    public async Task<string> UploadAssetAsync(byte[] data, string filename)
    {
        var now = DateTime.UtcNow.ToString("O");
        var contentType = filename.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
            ? "image/png"
            : "image/jpeg";

        using var content = new MultipartFormDataContent();

        var fileContent = new ByteArrayContent(data);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(fileContent, "assetData", filename);
        content.Add(new StringContent(Guid.NewGuid().ToString()), "deviceAssetId");
        content.Add(new StringContent("immich-seeder"), "deviceId");
        content.Add(new StringContent(now), "fileCreatedAt");
        content.Add(new StringContent(now), "fileModifiedAt");
        content.Add(new StringContent("false"), "isFavorite");

        var response = await httpClient.PostAsync("api/assets", content);
        response.EnsureSuccessStatusCode();

        var result = await JsonSerializer.DeserializeAsync<ImmichUploadResponse>(
            await response.Content.ReadAsStreamAsync(), JsonOptions);

        return result!.Id;
    }

    public async Task AddAssetsToAlbumAsync(string albumId, IEnumerable<string> assetIds)
    {
        var response = await httpClient.PutAsJsonAsync(
            $"api/albums/{albumId}/assets",
            new { ids = assetIds.ToArray() });
        response.EnsureSuccessStatusCode();
    }
}
