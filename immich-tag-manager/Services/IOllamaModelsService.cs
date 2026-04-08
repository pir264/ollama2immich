namespace ImmichTagManager.Services;

public interface IOllamaModelsService
{
    Task<List<string>> GetAvailableModelsAsync(string ollamaBaseUrl);
}
