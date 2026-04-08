namespace ImmichTagManager.Models;

public class AppSettings
{
    public string ImmichBaseUrl { get; set; } = "http://localhost:2283";
    public string ImmichApiKey { get; set; } = string.Empty;
    public string OllamaBaseUrl { get; set; } = "http://localhost:11434";
    public string OllamaModel { get; set; } = "gemma4";
    public string OllamaImageModel { get; set; } = "llava";
    public string TagPrompt { get; set; } = string.Empty;
    public string TagGeneratorPrompt { get; set; } = string.Empty;
    public string ImagePrompt { get; set; } = string.Empty;
    public string TagExistingPrompt { get; set; } = string.Empty;
    public int MaxGeneratedTags { get; set; } = 100;
    public int TagGeneratorDepth { get; set; } = 3;
    public int FeedSize { get; set; } = 10;
    public int ConcurrentAssets { get; set; } = 2;
    public int PageSize { get; set; } = 50;
    public List<OllamaImageInstance> OllamaImageInstances { get; set; } = [];

    public AppSettings Clone()
    {
        var copy = (AppSettings)MemberwiseClone();
        copy.OllamaImageInstances = OllamaImageInstances
            .Select(i => new OllamaImageInstance { BaseUrl = i.BaseUrl, Model = i.Model, DisplayName = i.DisplayName })
            .ToList();
        return copy;
    }
}
