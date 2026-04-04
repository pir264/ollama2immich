using System.Collections.Concurrent;
using ImmichSeeder.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(config => config
        .AddJsonFile("appsettings.json", optional: false)
        .AddJsonFile("appsettings.Local.json", optional: true)
        .AddEnvironmentVariables())
    .ConfigureServices((ctx, services) =>
    {
        var cfg = ctx.Configuration;
        var immichBase = cfg["Immich:BaseUrl"]?.TrimEnd('/') + "/";
        var immichKey  = cfg["Immich:ApiKey"] ?? string.Empty;

        if (string.IsNullOrWhiteSpace(immichKey))
        {
            Console.Error.WriteLine("Immich:ApiKey is required. Set it in appsettings.Local.json or via environment variable Immich__ApiKey.");
            Environment.Exit(1);
        }

        services.AddHttpClient<ImmichSeederService>(client =>
        {
            client.BaseAddress = new Uri(immichBase!);
            client.DefaultRequestHeaders.Add("x-api-key", immichKey);
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        services.AddHttpClient<WikimediaService>(client =>
        {
            client.DefaultRequestHeaders.UserAgent.ParseAdd("immich-seeder/1.0");
            client.Timeout = TimeSpan.FromSeconds(60);
        });
    })
    .Build();

var immich    = host.Services.GetRequiredService<ImmichSeederService>();
var wikimedia = host.Services.GetRequiredService<WikimediaService>();
var config    = host.Services.GetRequiredService<IConfiguration>();

int    count      = int.TryParse(config["Seeder:Count"], out var c)               ? c : 10;
int    concurrent = int.TryParse(config["Seeder:ConcurrentDownloads"], out var d) ? d : 3;
string albumName  = config["Seeder:AlbumName"] ?? "Test Photos";

Console.WriteLine($"Seeding {count} photo(s) into album \"{albumName}\"...");

var albumId = await immich.GetOrCreateAlbumAsync(albumName);

var assetIds  = new ConcurrentBag<string>();
var semaphore = new SemaphoreSlim(concurrent);
var tasks     = new List<Task>();
int uploaded  = 0;
int failed    = 0;

for (int i = 0; i < count; i++)
{
    await semaphore.WaitAsync();
    tasks.Add(Task.Run(async () =>
    {
        try
        {
            var (data, filename) = await wikimedia.GetRandomImageAsync();
            var assetId = await immich.UploadAssetAsync(data, filename);
            assetIds.Add(assetId);
            int n = Interlocked.Increment(ref uploaded);
            Console.WriteLine($"[{n}/{count}] Uploaded {filename}");
        }
        catch (Exception ex)
        {
            Interlocked.Increment(ref failed);
            Console.Error.WriteLine($"Failed: {ex.Message}");
        }
        finally
        {
            semaphore.Release();
        }
    }));
}

await Task.WhenAll(tasks);

if (assetIds.Count > 0)
    await immich.AddAssetsToAlbumAsync(albumId, assetIds);

Console.WriteLine($"Done. Uploaded: {uploaded}, Failed: {failed}");
