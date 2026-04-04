using System.Net;
using System.Text.Json;
using ImmichSeeder.Models;

namespace ImmichSeeder.Services;

public class WikimediaService(HttpClient httpClient)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly string[] ImageExtensions = [".jpg", ".jpeg", ".png"];

    /// <summary>
    /// Returns a random public-domain image from Wikimedia Commons.
    /// Retries internally until a valid JPEG/PNG is found.
    /// </summary>
    public async Task<(byte[] Data, string Filename)> GetRandomImageAsync()
    {
        while (true)
        {
            var titles = await GetRandomFileTitlesAsync();

            var imageTitles = titles
                .Where(t => ImageExtensions.Any(ext =>
                    t.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            if (imageTitles.Count == 0) continue;

            foreach (var title in imageTitles)
            {
                var url = await GetImageUrlAsync(title);
                if (url is null) continue;

                var response = await GetWithRetryAsync(url);
                var data = await response.Content.ReadAsByteArrayAsync();
                var filename = Path.GetFileName(new Uri(url).LocalPath);
                return (data, filename);
            }
        }
    }

    private async Task<List<string>> GetRandomFileTitlesAsync()
    {
        var response = await GetWithRetryAsync(
            "https://commons.wikimedia.org/w/api.php?action=query&list=random&rnnamespace=6&rnlimit=20&format=json");

        var result = await JsonSerializer.DeserializeAsync<WikimediaRandomResponse>(
            await response.Content.ReadAsStreamAsync(), JsonOptions);

        return result?.Query?.Random?.Select(f => f.Title).ToList() ?? [];
    }

    private async Task<string?> GetImageUrlAsync(string title)
    {
        var encodedTitle = Uri.EscapeDataString(title);
        var response = await GetWithRetryAsync(
            $"https://commons.wikimedia.org/w/api.php?action=query&prop=imageinfo&iiprop=url&titles={encodedTitle}&format=json");

        var result = await JsonSerializer.DeserializeAsync<WikimediaImageInfoResponse>(
            await response.Content.ReadAsStreamAsync(), JsonOptions);

        return result?.Query?.Pages?.Values
            .FirstOrDefault()?.Imageinfo?.FirstOrDefault()?.Url;
    }

    /// <summary>
    /// GETs <paramref name="url"/>, retrying on 429 by honouring the
    /// <c>Retry-After</c> header (or falling back to 5 seconds).
    /// </summary>
    private async Task<HttpResponseMessage> GetWithRetryAsync(string url)
    {
        while (true)
        {
            var response = await httpClient.GetAsync(url);

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                TimeSpan delay;
                if (response.Headers.RetryAfter?.Delta is TimeSpan delta)
                    delay = delta;
                else if (response.Headers.RetryAfter?.Date is DateTimeOffset until)
                    delay = (until - DateTimeOffset.UtcNow).Add(TimeSpan.FromSeconds(1)); // Add 1s buffer
                else
                    delay = TimeSpan.FromSeconds(5);

                Console.Error.WriteLine($"Rate-limited by Wikimedia. Waiting {delay.TotalSeconds:F0}s...");
                await Task.Delay(delay);
                continue;
            }

            response.EnsureSuccessStatusCode();
            return response;
        }
    }
}
