using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ollama2immich.Services;

bool resetMode = args.Contains("--reset", StringComparer.OrdinalIgnoreCase);

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(config => config
        .AddJsonFile("appsettings.json", optional: false)
        .AddJsonFile("appsettings.Local.json", optional: true)
        .AddEnvironmentVariables())
    .ConfigureServices((ctx, services) =>
    {
        var cfg = ctx.Configuration;

        var immichBase  = cfg["Immich:BaseUrl"]?.TrimEnd('/') + "/";
        var immichKey   = cfg["Immich:ApiKey"] ?? string.Empty;
        var ollamaBase   = cfg["Ollama:BaseUrl"]?.TrimEnd('/') + "/";
        var ollamaModel  = cfg["Ollama:Model"] ?? "llava";
        var ollamaPrompt = cfg["Ollama:Prompt"] ?? string.Empty;

        if (!resetMode && string.IsNullOrWhiteSpace(ollamaPrompt))
        {
            Console.Error.WriteLine("ERROR: Ollama:Prompt is not set in appsettings.json.");
            Environment.Exit(1);
        }

        if (string.IsNullOrWhiteSpace(immichKey))
        {
            Console.Error.WriteLine("ERROR: Immich:ApiKey is not set in appsettings.json.");
            Environment.Exit(1);
        }

        services.AddHttpClient<ImmichService>(client =>
        {
            client.BaseAddress = new Uri(immichBase!);
            client.DefaultRequestHeaders.Add("x-api-key", immichKey);
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        if (!resetMode)
        {
            services.AddSingleton(sp =>
            {
                var http = new HttpClient
                {
                    BaseAddress = new Uri(ollamaBase!),
                    Timeout = TimeSpan.FromMinutes(5)
                };
                return new OllamaService(http, ollamaModel, ollamaPrompt);
            });
        }
    })
    .Build();

var immich = host.Services.GetRequiredService<ImmichService>();
var config = host.Services.GetRequiredService<IConfiguration>();

if (resetMode)
{
    await RunResetAsync(immich);
    return;
}

var ollama = host.Services.GetRequiredService<OllamaService>();

int concurrent = int.TryParse(config["Processing:ConcurrentAssets"], out var c) ? c : 2;
int pageSize   = int.TryParse(config["Processing:PageSize"], out var p) ? p : 50;

Console.WriteLine("ollama2immich starting...");
Console.WriteLine($"  Immich : {config["Immich:BaseUrl"]}");
Console.WriteLine($"  Ollama : {config["Ollama:BaseUrl"]} (model: {config["Ollama:Model"]})");
Console.WriteLine($"  Concurrency: {concurrent}, Page size: {pageSize}");
Console.WriteLine();

// Cache existing tags to avoid repeated GET /api/tags
var existingTags = (await immich.GetTagsAsync())
    .ToDictionary(t => t.Name.ToLowerInvariant(), t => t.Id);

int processed = 0, skipped = 0, failed = 0;

var semaphore = new SemaphoreSlim(concurrent);
var tasks = new List<Task>();

await foreach (var asset in immich.GetAllAssetsAsync(pageSize))
{
    // Only process image assets
    if (!string.Equals(asset.Type, "IMAGE", StringComparison.OrdinalIgnoreCase))
    {
        skipped++;
        continue;
    }

    // Skip if already has a description
    if (!string.IsNullOrWhiteSpace(asset.ExifInfo?.Description))
    {
        skipped++;
        continue;
    }

    await semaphore.WaitAsync();
    var assetId = asset.Id;

    tasks.Add(Task.Run(async () =>
    {
        try
        {
            Console.WriteLine($"[->] Processing {assetId}");

            var thumbnail = await immich.GetThumbnailAsync(assetId);
            var (description, tags) = await ollama.AnalyzeImageAsync(thumbnail);

            await immich.UpdateDescriptionAsync(assetId, description);

            foreach (var tag in tags)
            {
                var tagKey = tag.ToLowerInvariant();
                if (!existingTags.TryGetValue(tagKey, out var tagId))
                {
                    var created = await immich.CreateTagAsync(tag);
                    tagId = created.Id;
                    existingTags[tagKey] = tagId;
                }
                await immich.AssignTagToAssetAsync(tagId, assetId);
            }

            Console.WriteLine($"[OK] {assetId}: \"{description}\" | tags: {string.Join(", ", tags)}");
            Interlocked.Increment(ref processed);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[FAIL] {assetId}: {ex.Message}");
            Interlocked.Increment(ref failed);
        }
        finally
        {
            semaphore.Release();
        }
    }));
}

await Task.WhenAll(tasks);

Console.WriteLine();
Console.WriteLine($"Done. Processed: {processed}, Skipped: {skipped}, Failed: {failed}");

static async Task RunResetAsync(ImmichService immich)
{
    Console.WriteLine("=== RESET MODE ===");
    Console.WriteLine("Clearing descriptions and deleting all tags...");
    Console.WriteLine();

    int cleared = 0;
    await foreach (var asset in immich.GetAllAssetsAsync())
    {
        if (!string.Equals(asset.Type, "IMAGE", StringComparison.OrdinalIgnoreCase))
            continue;
        if (string.IsNullOrWhiteSpace(asset.ExifInfo?.Description))
            continue;

        Console.WriteLine($"[CLEAR] {asset.Id}");
        await immich.UpdateDescriptionAsync(asset.Id, "");
        cleared++;
    }

    var tags = await immich.GetTagsAsync();
    int deleted = 0, skippedTags = 0;
    foreach (var tag in tags)
    {
        try
        {
            Console.WriteLine($"[DELETE TAG] {tag.Name} ({tag.Id})");
            await immich.DeleteTagAsync(tag.Id);
            deleted++;
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"[SKIP TAG] {tag.Name}: {ex.StatusCode} — skipped");
            skippedTags++;
        }
    }

    Console.WriteLine();
    Console.WriteLine($"Reset complete. Cleared {cleared} description(s), deleted {deleted} tag(s), skipped {skippedTags} tag(s).");
}
