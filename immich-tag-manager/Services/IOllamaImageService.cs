namespace ImmichTagManager.Services;

public interface IOllamaImageService
{
    Task<(string Description, string[] Tags)> AnalyzeImageAsync(byte[] imageBytes);
    Task<string[]> SelectTagsAsync(byte[] imageBytes, string[] tagNames);
}
