using System.Text.Json;
using ImmichTagManager.Models;

namespace ImmichTagManager.Services;

public class AppSettingsService : IAppSettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string _persistPath;
    private AppSettings _current;
    private readonly object _lock = new();

    public AppSettingsService(IConfiguration configuration, IWebHostEnvironment env)
    {
        _persistPath = Path.Combine(env.ContentRootPath, "appsettings.user.json");
        _current = LoadFromConfiguration(configuration);

        if (File.Exists(_persistPath))
        {
            try
            {
                var json = File.ReadAllText(_persistPath);
                var saved = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
                if (saved is not null)
                    _current = Merge(_current, saved);
            }
            catch { /* corrupt file: gebruik config defaults */ }
        }
    }

    public AppSettings GetSettings()
    {
        lock (_lock)
            return _current.Clone();
    }

    public async Task SaveSettingsAsync(AppSettings settings)
    {
        var snapshot = settings.Clone();
        var json = JsonSerializer.Serialize(snapshot, JsonOptions);
        await File.WriteAllTextAsync(_persistPath, json);
        lock (_lock)
            _current = snapshot;
    }

    private static AppSettings LoadFromConfiguration(IConfiguration cfg) => new()
    {
        ImmichBaseUrl         = cfg["Immich:BaseUrl"]?.TrimEnd('/') ?? "http://localhost:2283",
        ImmichApiKey          = cfg["Immich:ApiKey"] ?? string.Empty,
        OllamaBaseUrl         = cfg["Ollama:BaseUrl"]?.TrimEnd('/') ?? "http://localhost:11434",
        OllamaModel           = cfg["Ollama:Model"] ?? "gemma4",
        OllamaImageModel      = cfg["Ollama:ImageModel"] ?? "llava",
        TagPrompt             = cfg["Ollama:TagPrompt"] ?? string.Empty,
        TagGeneratorPrompt    = cfg["Ollama:TagGeneratorPrompt"] ?? string.Empty,
        ImagePrompt           = cfg["Ollama:ImagePrompt"] ?? string.Empty,
        TagExistingPrompt     = cfg["Ollama:TagExistingPrompt"] ?? string.Empty,
        MaxGeneratedTags      = int.TryParse(cfg["Ollama:MaxGeneratedTags"], out var m) ? m : 100,
        TagGeneratorDepth     = int.TryParse(cfg["Ollama:TagGeneratorDepth"], out var d) ? d : 3,
        FeedSize              = int.TryParse(cfg["ImageAnalysis:FeedSize"], out var f) ? f : 10,
        ConcurrentAssets      = int.TryParse(cfg["ImageAnalysis:ConcurrentAssets"], out var c) ? c : 2,
        PageSize              = int.TryParse(cfg["ImageAnalysis:PageSize"], out var p) ? p : 50,
        OllamaImageInstances  = ReadImageInstances(cfg),
    };

    private static List<OllamaImageInstance> ReadImageInstances(IConfiguration cfg)
    {
        return cfg.GetSection("Ollama:ImageInstances").GetChildren()
            .Select(c => new OllamaImageInstance
            {
                BaseUrl     = c["BaseUrl"]?.TrimEnd('/') ?? string.Empty,
                Model       = c["Model"] ?? string.Empty,
                DisplayName = c["DisplayName"] ?? string.Empty,
            })
            .Where(i => !string.IsNullOrWhiteSpace(i.BaseUrl))
            .ToList();
    }

    private static AppSettings Merge(AppSettings defaults, AppSettings saved) => new()
    {
        ImmichBaseUrl        = NonEmpty(saved.ImmichBaseUrl, defaults.ImmichBaseUrl),
        ImmichApiKey         = saved.ImmichApiKey,
        OllamaBaseUrl        = NonEmpty(saved.OllamaBaseUrl, defaults.OllamaBaseUrl),
        OllamaModel          = NonEmpty(saved.OllamaModel, defaults.OllamaModel),
        OllamaImageModel     = NonEmpty(saved.OllamaImageModel, defaults.OllamaImageModel),
        TagPrompt            = NonEmpty(saved.TagPrompt, defaults.TagPrompt),
        TagGeneratorPrompt   = NonEmpty(saved.TagGeneratorPrompt, defaults.TagGeneratorPrompt),
        ImagePrompt          = NonEmpty(saved.ImagePrompt, defaults.ImagePrompt),
        TagExistingPrompt    = NonEmpty(saved.TagExistingPrompt, defaults.TagExistingPrompt),
        MaxGeneratedTags     = saved.MaxGeneratedTags > 0 ? saved.MaxGeneratedTags : defaults.MaxGeneratedTags,
        TagGeneratorDepth    = saved.TagGeneratorDepth > 0 ? saved.TagGeneratorDepth : defaults.TagGeneratorDepth,
        FeedSize             = saved.FeedSize > 0 ? saved.FeedSize : defaults.FeedSize,
        ConcurrentAssets     = saved.ConcurrentAssets > 0 ? saved.ConcurrentAssets : defaults.ConcurrentAssets,
        PageSize             = saved.PageSize > 0 ? saved.PageSize : defaults.PageSize,
        OllamaImageInstances = saved.OllamaImageInstances.Count > 0
            ? saved.OllamaImageInstances
            : defaults.OllamaImageInstances,
    };

    private static string NonEmpty(string? candidate, string fallback)
        => string.IsNullOrWhiteSpace(candidate) ? fallback : candidate;
}
